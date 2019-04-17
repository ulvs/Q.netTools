using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QTMRealTimeSDK;
using QTMRealTimeSDK.Data;
using System.Threading;
using System.IO;

namespace SixDoFLogger
{
    class Program
    {



        static void Main(string[] args)
        {
            //Constants
            string filenameCSV = DateTime.Now.ToShortDateString() + "_" + (int)DateTime.Now.TimeOfDay.TotalSeconds + ".csv";
            string filenameReadable = DateTime.Now.ToShortDateString() + "_" + (int)DateTime.Now.TimeOfDay.TotalSeconds + ".txt";
            const bool logReadable = false;
            const bool logCsv = true;
            const bool verbose = false;
            const bool verboseCsv = false;
            string ipAddress = "127.0.0.1";

            DateTime lastBlip = new DateTime();
            lastBlip = DateTime.Now;
            int frameNumber = 0;
            RTProtocol mRtProtocol = new RTProtocol();

            while (true)
            {
                if (!mRtProtocol.IsConnected())
                {
                    if (!mRtProtocol.Connect(ipAddress))
                    {
                        Console.WriteLine("QTM: Trying to connect");
                        Thread.Sleep(1000);

                    }

                }
                else
                {
                    Console.WriteLine("QTM: Connected");
                    break;
                }
            }
            if (mRtProtocol.Settings6DOF == null)
            {
                if (!mRtProtocol.Get6dSettings())
                {
                    Console.WriteLine("QTM: Trying to get 6DOF settings");
                    Thread.Sleep(500);

                }
                Console.WriteLine("QTM: 6DOF settings available");

                //List<ComponentType> componentsToStream = new List<ComponentType>
                //{
                //    ComponentType.Component6dEulerResidual,
                //    ComponentType.ComponentTimecode
                //};

                mRtProtocol.StreamAllFrames(ComponentType.Component6dEulerResidual);
                Console.WriteLine("QTM: Starting to stream 6DOF data");
                Thread.Sleep(500);
            }
            string fileName = DateTime.Now.ToShortDateString() + "_" + (int)DateTime.Now.TimeOfDay.TotalSeconds + ".csv";
            PacketType packetType;
            List<Q6DOFEuler> previousFrame6dData = new List<Q6DOFEuler>();

            while (previousFrame6dData.Count == 0)
            {
                mRtProtocol.ReceiveRTPacket(out packetType, false);
                if (packetType == PacketType.PacketData)
                    previousFrame6dData = mRtProtocol.GetRTPacket().Get6DOFEulerResidualData();
            }

            var csvHeader = "Frame;Body;Type;Magnitude;X;Y;Z\n";
            using (var writer = File.AppendText(filenameCSV))
            {
                writer.Write(csvHeader);
            }

            while (true)
            {
                mRtProtocol.ReceiveRTPacket(out packetType, false);
                if (packetType == PacketType.PacketData)
                {
                    StringBuilder writeBufferReadable = new StringBuilder();
                    StringBuilder writeBufferCSV = new StringBuilder();

                    var frame6dData = mRtProtocol.GetRTPacket().Get6DOFEulerResidualData();
                    var packet = mRtProtocol.GetRTPacket();
                    frameNumber = packet.Frame;
                    if (frame6dData != null)
                    {
                        for (int body = 0; body < frame6dData.Count; body++)
                        {
                            var sixDofBody = frame6dData[body];
                            var prevSexDofBody = previousFrame6dData[body];
                            var bodySetting = mRtProtocol.Settings6DOF.Bodies[body];

                            if (float.IsNaN(sixDofBody.Residual) && float.IsNaN(prevSexDofBody.Residual))
                            {

                            }
                            else if (!float.IsNaN(sixDofBody.Residual) && float.IsNaN(prevSexDofBody.Residual))
                            {
                                writeBufferReadable.AppendFormat("{0} Body: {1} appeard with coordinates {2:F2} {3:F2} {4:F2}", frameNumber, bodySetting.Name, sixDofBody.Position.X / 1000, sixDofBody.Position.Y / 1000, sixDofBody.Position.Z / 1000);

                                writeBufferCSV.AppendFormat("{0};{1};A;NaN;{2:F2};{3:F2};{4:F2}", frameNumber, bodySetting.Name, sixDofBody.Position.X / 1000, sixDofBody.Position.Y / 1000, sixDofBody.Position.Z / 1000);
                                //Console.WriteLine("Body: " + bodySetting.Name + " appeared");
                            }
                            else if (float.IsNaN(sixDofBody.Residual) && !float.IsNaN(prevSexDofBody.Residual))
                            {
                                writeBufferReadable.AppendFormat("{0} Body: {1} disappeared with coordinates {2:F2} {3:F2} {4:F2}", frameNumber, bodySetting.Name, prevSexDofBody.Position.X / 1000, prevSexDofBody.Position.Y / 1000, prevSexDofBody.Position.Z / 1000);
                                writeBufferCSV.AppendFormat("{0};{1};D;NaN;{2:F2};{3:F2};{4:F2}", frameNumber, bodySetting.Name, prevSexDofBody.Position.X / 1000, prevSexDofBody.Position.Y / 1000, prevSexDofBody.Position.Z / 1000);

                                //Console.WriteLine("Body: " + bodySetting.Name + " disappeared");
                            }
                            else if (!float.IsNaN(sixDofBody.Residual) && !float.IsNaN(prevSexDofBody.Residual))
                            {
                                var movementX = sixDofBody.Position.X - prevSexDofBody.Position.X;
                                var movementY = sixDofBody.Position.Y - prevSexDofBody.Position.Y;
                                var movementZ = sixDofBody.Position.Z - prevSexDofBody.Position.Z;
                                var movementABS = Math.Sqrt(Math.Pow(movementX, 2) + Math.Pow(movementY, 2) + Math.Pow(movementZ, 2));
                                var angMovement1 = Math.Abs(sixDofBody.Rotation.First - prevSexDofBody.Rotation.First);
                                var angMovement2 = Math.Abs(sixDofBody.Rotation.Second - prevSexDofBody.Rotation.Second);
                                var angMovement3 = Math.Abs(sixDofBody.Rotation.Third - prevSexDofBody.Rotation.Third);
                                var angMoveMax = Math.Max(angMovement1, Math.Max(angMovement2, angMovement3));
                                if (movementABS > 100)
                                {
                                    writeBufferReadable.AppendFormat("{0} Body: {1} jumped {2:F}mm at {3:F2} {4:F2} {5:F2}", frameNumber, bodySetting.Name, movementABS, sixDofBody.Position.X / 1000, sixDofBody.Position.Y / 1000, sixDofBody.Position.Z / 1000);
                                    writeBufferCSV.AppendFormat("{0};{1};J;{2:F2};{3:F2};{4:F2};{5:F2}", frameNumber, bodySetting.Name, movementABS, sixDofBody.Position.X / 1000, sixDofBody.Position.Y / 1000, sixDofBody.Position.Z / 1000);

                                    //Console.WriteLine("Body : " + bodySetting.Name + " movement:" + movementABS);
                                }
                                if (angMoveMax > 10)
                                {
                                    writeBufferReadable.AppendFormat("{0} Body: {1} flipped {2:F0} deg at {2:F2} {3:F2} {4:F2}", frameNumber, bodySetting.Name, angMoveMax, sixDofBody.Position.X / 1000, sixDofBody.Position.Y / 1000, sixDofBody.Position.Z / 1000);
                                    writeBufferCSV.AppendFormat("{0};{1};F;{2:F2};{3:F2};{4:F2};{5:F2}", frameNumber, bodySetting.Name, movementABS, sixDofBody.Position.X / 1000, sixDofBody.Position.Y / 1000, sixDofBody.Position.Z / 1000);

                                    //Console.WriteLine("Body : {0:S} angular movement First: {1:F} Second: {2:F} Third: {3:F}", bodySetting.Name, angMovement1,angMovement2,angMovement3);
                                    //Console.WriteLine("Frame:{0:D5} Body:{1,20} X:{2,7:F1} Y:{3,7:F1} Z:{4,7:F1} First Angle:{5,7:F1} Second Angle:{6,7:F1} Third Angle:{7,7:F1} Residual:{8,7:F1}
                                }



                            }
                            if (writeBufferCSV.Length > 0)
                            {
                                if (logCsv)
                                {
                                    using (var writer = File.AppendText(filenameCSV))
                                    {
                                        writer.WriteLine(writeBufferCSV);

                                    }
                                }
                                if (logReadable)
                                {
                                    using (var writer = File.AppendText(filenameReadable))
                                    {
                                        writer.WriteLine(writeBufferReadable);

                                    }
                                }
                                if (verbose)
                                {
                                    Console.WriteLine(writeBufferReadable);
                                }
                                if (verboseCsv)
                                {
                                    Console.WriteLine(writeBufferCSV);
                                }

                                writeBufferReadable.Clear();
                                writeBufferCSV.Clear();
                            }


                        }
                        previousFrame6dData = frame6dData;
                    }
                }



                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey(false).Key == ConsoleKey.Escape)
                        break;
                    
                }
                if (lastBlip.AddSeconds(1) < DateTime.Now)
                {
                    Console.WriteLine("Loggind, current frame " + frameNumber);
                    lastBlip = DateTime.Now;

                }

            }



        }
    }


}


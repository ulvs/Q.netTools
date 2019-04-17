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
            RTProtocol mRtProtocol = new RTProtocol();
            string mIpAddress = "127.0.0.1";
            while (true)
            {
                if (!mRtProtocol.IsConnected())
                {
                    if (!mRtProtocol.Connect(mIpAddress))
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

                List<ComponentType> componentsToStream = new List<ComponentType>();
                componentsToStream.Add(ComponentType.Component6dEulerResidual);
                componentsToStream.Add(ComponentType.ComponentTimecode);
                mRtProtocol.StreamAllFrames(componentsToStream);
                Console.WriteLine("QTM: Starting to stream 6DOF data");
                Thread.Sleep(500);
            }
            string fileName = DateTime.Now.ToShortDateString() + "_" + (int)DateTime.Now.TimeOfDay.TotalSeconds + "_FILL.csv";
            PacketType packetType;
            List<Q6DOFEuler> previousFrame6dData = new List<Q6DOFEuler>();

            while (previousFrame6dData.Count == 0)
            {
                mRtProtocol.ReceiveRTPacket(out packetType, false);
                if (packetType == PacketType.PacketData)
                    previousFrame6dData = mRtProtocol.GetRTPacket().Get6DOFEulerResidualData();
            }

            while (true)
            {
                mRtProtocol.ReceiveRTPacket(out packetType, false);
                if (packetType == PacketType.PacketData)
                {

                    var frame6dData = mRtProtocol.GetRTPacket().Get6DOFEulerResidualData();
                    var packet = mRtProtocol.GetRTPacket();
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
                                Console.WriteLine("Body: " + bodySetting.Name + " appeard");
                            }
                            else if (float.IsNaN(sixDofBody.Residual) && !float.IsNaN(prevSexDofBody.Residual))
                            {
                                Console.WriteLine("Body: " + bodySetting.Name + " disapeard");
                            }
                            else if (float.IsNaN(sixDofBody.Residual) && float.IsNaN(prevSexDofBody.Residual))
                            {

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
            }

            //while (true)
            //{
            //    mRtProtocol.ReceiveRTPacket(out packetType, false);

            //    // Handle data packet
            //    if (packetType == PacketType.PacketData)
            //    {
            //        var sixDofData = mRtProtocol.GetRTPacket().Get6DOFEulerResidualData();
            //        var timeCodePacket = mRtProtocol.GetRTPacket().GetTimecodeData();
            //        var packet = mRtProtocol.GetRTPacket();
            //        if (sixDofData != null)
            //        {
            //            using (var writer = File.AppendText(fileName))
            //            {
            //                writer.Write(mRtProtocol.GetRTPacket().Frame + ",");
            //                writer.Write(mRtProtocol.GetRTPacket().DropRate + ",");
            //                writer.Write(mRtProtocol.GetRTPacket().OutOfSyncRate + ",");
            //                // Print out the available 6DOF data.
            //                for (int body = 0; body < sixDofData.Count; body++)
            //                {
            //                    var sixDofBody = sixDofData[body];
            //                    var bodySetting = mRtProtocol.Settings6DOF.Bodies[body];

            //                    //writer.Write(sixDofBody.Position.X + "," + sixDofBody.Position.Y + "," + sixDofBody.Position.Z + "," + sixDofBody.Residual + ",");
            //                    //writer.Write(sixDofBody.Position.X + ",");
            //                    if (sixDofBody.Position.X.ToString().Contains("NaN"))
            //                    {
            //                        writer.Write("0" + ",");
            //                    }
            //                    else
            //                    {
            //                        writer.Write("1" + ",");
            //                    }
            //                    //Console.WriteLine("Frame:{0:D5} Body:{1,20} X:{2,7:F1} Y:{3,7:F1} Z:{4,7:F1} First Angle:{5,7:F1} Second Angle:{6,7:F1} Third Angle:{7,7:F1} Residual:{8,7:F1}",
            //                    //mRtProtocol.GetRTPacket().Frame,
            //                    //bodySetting.Name,
            //                    //sixDofBody.Position.X, sixDofBody.Position.Y, sixDofBody.Position.Z,
            //                    //sixDofBody.Rotation.First, sixDofBody.Rotation.Second, sixDofBody.Rotation.Third,
            //                    //sixDofBody.Residual);
            //                    //}
            //                    //Console.WriteLine(mRtProtocol.GetRTPacket().Frame + "," + timeCodePacket.ToString());

            //                }
            //                writer.WriteLine();
            //            }
            //            using (var writerXYZ = File.AppendText(fileNameXYZ))
            //            {
            //                writerXYZ.Write(mRtProtocol.GetRTPacket().Frame + ",");
            //                // Print out the available 6DOF data.
            //                for (int body = 0; body < sixDofData.Count; body++)
            //                {
            //                    var sixDofBody = sixDofData[body];
            //                    var bodySetting = mRtProtocol.Settings6DOF.Bodies[body];

            //                    writerXYZ.Write(sixDofBody.Position.X + "," + sixDofBody.Position.Y + "," + sixDofBody.Position.Z + ",");
            //                }

            //                writerXYZ.WriteLine();
            //            }

            //        }
            //        if (Console.KeyAvailable)
            //        {
            //            if (Console.ReadKey(false).Key == ConsoleKey.Escape)
            //                break;
            //        }


        }
    }


}


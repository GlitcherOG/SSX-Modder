﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSX_Modder.Utilities;

namespace SSX_Modder.FileHandlers
{
    public class MPFModelHandler
    {
        public int HeaderCount;
        public int HeaderSize;
        public List<MPFModelHeader> ModelList = new List<MPFModelHeader>();

        public void load(string path)
        {
            using (Stream stream = File.Open(path, FileMode.Open))
            {
                stream.Position += 4;
                HeaderCount = StreamUtil.ReadInt16(stream);
                stream.Position = 0;
                //Load Headers
                for (int i = 0; i < HeaderCount; i++)
                {
                    MPFModelHeader modelHeader = new MPFModelHeader();

                    modelHeader.U1 = StreamUtil.ReadInt32(stream);
                    modelHeader.SubHeaders = StreamUtil.ReadInt16(stream);
                    modelHeader.HeaderSize = StreamUtil.ReadInt16(stream);
                    modelHeader.FileStart = StreamUtil.ReadInt32(stream);

                    modelHeader.FileName = StreamUtil.ReadString(stream, 16).Replace("\0", "");
                    modelHeader.DataOffset = StreamUtil.ReadInt32(stream);
                    modelHeader.EntrySize = StreamUtil.ReadInt32(stream);
                    modelHeader.ModelsOffset = StreamUtil.ReadInt32(stream);
                    modelHeader.RotationInfo = StreamUtil.ReadInt32(stream);
                    modelHeader.U8 = StreamUtil.ReadInt32(stream);
                    modelHeader.DataStart = StreamUtil.ReadInt32(stream);

                    modelHeader.Chunks = StreamUtil.ReadInt16(stream);
                    modelHeader.ModelCount = StreamUtil.ReadInt16(stream);
                    modelHeader.U22 = StreamUtil.ReadInt16(stream);
                    modelHeader.BodyObjectsCount = StreamUtil.ReadByte(stream);
                    modelHeader.U23 = StreamUtil.ReadByte(stream);
                    stream.Position += 4;
                    ModelList.Add(modelHeader);
                }

                //Read Matrix
                int StartPos = ModelList[0].FileStart;
                for (int i = 0; i < ModelList.Count; i++)
                {
                    stream.Position = StartPos + ModelList[i].DataOffset;
                    MPFModelHeader modelHandler = ModelList[i];
                    modelHandler.Matrix = StreamUtil.ReadBytes(stream, ModelList[i].EntrySize);
                    RefpackHandler refpackHandler = new RefpackHandler();
                    modelHandler.Matrix = refpackHandler.Decompress(modelHandler.Matrix);
                    ModelList[i] = modelHandler;
                }
            }

            //Read Matrix Data
            for (int i = 0; i < ModelList.Count; i++)
            {
                Stream streamMatrix = new MemoryStream();
                var Model = ModelList[i];
                streamMatrix.Write(ModelList[i].Matrix, 0, ModelList[i].Matrix.Length);

                //Read BodyObjects
                Model.bodyObjectsList = new List<BodyObjects>();
                streamMatrix.Position = 0;
                for (int b = 0; b < Model.BodyObjectsCount; b++)
                {
                    BodyObjects body = new BodyObjects();
                    body.Name = StreamUtil.ReadString(streamMatrix, 4);
                    body.Float1 = StreamUtil.ReadFloat(streamMatrix);
                    body.Float2 = StreamUtil.ReadFloat(streamMatrix);
                    body.Float3 = StreamUtil.ReadFloat(streamMatrix);
                    Model.bodyObjectsList.Add(body);
                }

                //Read ModelData
                streamMatrix.Position = Model.ModelsOffset;
                var models = new List<ModelData>();
                for (int b = 0; b < Model.ModelCount; b++)
                {
                    ModelData modelData = new ModelData();
                    modelData.modelName = StreamUtil.ReadString(streamMatrix, 16);
                    modelData.Int1 = StreamUtil.ReadInt32(streamMatrix);
                    modelData.Float2 = StreamUtil.ReadFloat(streamMatrix);
                    modelData.Float3 = StreamUtil.ReadFloat(streamMatrix);
                    modelData.Float4 = StreamUtil.ReadFloat(streamMatrix);
                    models.Add(modelData);
                }
                Model.models = models;

                //Read RotationData?
                streamMatrix.Position = Model.RotationInfo;
                var Verties = new List<Vertex3>();
                for (int a = 0; a < Model.U23; a++)
                {
                    Vertex3 v = new Vertex3();
                    v.X = StreamUtil.ReadFloat(streamMatrix);
                    v.Y = StreamUtil.ReadFloat(streamMatrix);
                    v.Z = StreamUtil.ReadFloat(streamMatrix);
                    streamMatrix.Position += 4;
                    Verties.Add(v);
                }
                Model.Unkown = Verties;


                streamMatrix.Position = Model.DataStart;
                for (int a = 0; a < Model.models.Count; a++)
                {
                    var ModelData = Model.models[a];
                    ModelData.chunks = new List<Chunk>();

                    ModelData.vertices = new List<Vertex3>();
                    ModelData.Strips = new List<int>();
                    ModelData.uv = new List<UV>();
                    var Chunks = ModelData.chunks;
                    for (int b = 0; b < Model.Chunks; b++)
                    {
                        streamMatrix.Position += 48;
                        var Chunk = new Chunk();
                        Chunk.StripCount = StreamUtil.ReadInt32(streamMatrix);
                        Chunk.u1 = StreamUtil.ReadInt32(streamMatrix);
                        Chunk.NormalCount = StreamUtil.ReadInt32(streamMatrix);
                        Chunk.VertexCount = StreamUtil.ReadInt32(streamMatrix);

                        //Load Strip Count
                        List<int> TempStrips = ModelData.Strips;
                        for (int d = 0; d < Chunk.StripCount; d++)
                        {
                            TempStrips.Add(StreamUtil.ReadInt32(streamMatrix));
                            streamMatrix.Position += 12;
                        }

                        // Incrament the stripcount
                        List<int> strip2 = new List<int>();
                        strip2.Add(0);
                        foreach (var item in TempStrips)
                        {
                            strip2.Add(strip2[strip2.Count - 1] + item);
                        }
                        streamMatrix.Position += 16;
                        ModelData.Strips = strip2;

                        if (Chunk.NormalCount != 0)
                        {
                            streamMatrix.Position += 48;
                            List<UV> UVs = ModelData.uv;
                            for (int c = 0; c < Chunk.VertexCount; c++)
                            {
                                UV tempUV = new UV();
                                tempUV.X = StreamUtil.ReadInt16(streamMatrix);
                                tempUV.Y = StreamUtil.ReadInt16(streamMatrix);
                                UVs.Add(tempUV);
                            }
                            ModelData.uv = UVs;
                            streamMatrix.Position += 16 - (streamMatrix.Position % 16);
                        }

                        if (Chunk.NormalCount != 0)
                        {
                            streamMatrix.Position += 48;
                            List<UV> UVs = ModelData.uv;
                            for (int c = 0; c < Chunk.VertexCount; c++)
                            {
                                streamMatrix.Position += 6;
                                //UV tempUV = new UV();
                                //tempUV.X = StreamUtil.ReadInt16(streamMatrix);
                                //tempUV.Y = StreamUtil.ReadInt16(streamMatrix);
                                //UVs.Add(tempUV);
                            }
                            ModelData.uv = UVs;
                            streamMatrix.Position += 16 - (streamMatrix.Position % 16);
                        }

                        if (Chunk.VertexCount != 0)
                        {
                            streamMatrix.Position += 48;
                            List<Vertex3> vertices = ModelData.vertices;
                            for (int c = 0; c < Chunk.VertexCount; c++)
                            {
                                Vertex3 vertex = new Vertex3();
                                vertex.X = StreamUtil.ReadFloat(streamMatrix);
                                vertex.Y = StreamUtil.ReadFloat(streamMatrix);
                                vertex.Z = StreamUtil.ReadFloat(streamMatrix);
                                vertices.Add(vertex);
                            }
                            ModelData.vertices = vertices;
                            streamMatrix.Position += 16 - (streamMatrix.Position % 16);

                            streamMatrix.Position += 64;
                        }
                        Chunks.Add(Chunk);
                    }
                    ModelData.faces = new List<Face>();
                    int localIndex = 0;
                    //Make Faces
                    for (int b = 0; b < ModelData.vertices.Count; b++)
                    {
                        if (InsideSplits(b, ModelData.Strips))
                        {
                            localIndex = 1;
                            continue;
                        }
                        if (localIndex < 2)
                        {
                            localIndex++;
                            continue;
                        }

                        ModelData.faces.Add(CreateFaces(b));
                        localIndex++;
                    }
                    ModelData.chunks = Chunks;
                    Model.models[a] = ModelData;
                    ModelList[i] = Model;
                }

                streamMatrix.Dispose();
                streamMatrix.Close();
            }
        }

        //public void loadSSX3(string path)
        //{
        //    using (Stream stream = File.Open(path, FileMode.Open))
        //    {
        //        //Load Headers
        //        while (true)
        //        {
        //            MPFModelHeader modelHeader = new MPFModelHeader();

        //            modelHeader.U1 = StreamUtil.ReadInt32(stream);
        //            modelHeader.SubHeaders = StreamUtil.ReadInt16(stream);
        //            modelHeader.HeaderSize = StreamUtil.ReadInt16(stream);
        //            modelHeader.FileStart = StreamUtil.ReadInt32(stream);

        //            int Test = StreamUtil.ReadInt32(stream);
        //            if (Test == 0)
        //            {
        //                ModelList.Add(modelHeader);
        //                break;
        //            }
        //            else
        //            {
        //                stream.Position -= 4;
        //            }

        //            modelHeader.ModelName = StreamUtil.ReadString(stream, 16).Replace("\0", "");
        //            modelHeader.DataOffset = StreamUtil.ReadInt32(stream);
        //            modelHeader.EntrySize = StreamUtil.ReadInt32(stream);
        //            modelHeader.NameOffset = StreamUtil.ReadInt32(stream);
        //            modelHeader.U7 = StreamUtil.ReadInt32(stream);
        //            modelHeader.U8 = StreamUtil.ReadInt32(stream);
        //            modelHeader.U9 = StreamUtil.ReadInt32(stream);
        //            modelHeader.U10 = StreamUtil.ReadInt32(stream);
        //            modelHeader.U11 = StreamUtil.ReadInt32(stream);
        //            modelHeader.U12 = StreamUtil.ReadInt32(stream);
        //            modelHeader.U13 = StreamUtil.ReadInt32(stream);
        //            modelHeader.U14 = StreamUtil.ReadInt32(stream);
        //            modelHeader.U15 = StreamUtil.ReadInt32(stream);
        //            modelHeader.U16 = StreamUtil.ReadInt32(stream);

        //            modelHeader.U17 = StreamUtil.ReadInt16(stream);
        //            modelHeader.U18 = StreamUtil.ReadInt16(stream);
        //            modelHeader.U19 = StreamUtil.ReadInt16(stream);
        //            modelHeader.U20 = StreamUtil.ReadInt16(stream);
        //            modelHeader.BodyObjectsCount = StreamUtil.ReadInt16(stream);
        //            modelHeader.U22 = StreamUtil.ReadInt16(stream);
        //            modelHeader.U23 = StreamUtil.ReadInt16(stream);
        //            modelHeader.U24 = StreamUtil.ReadInt16(stream);
        //            ModelList.Add(modelHeader);
        //        }

        //        //Read Matrix And Decompress
        //        int StartPos = ModelList[0].FileStart;
        //        for (int i = 0; i < ModelList.Count - 1; i++)
        //        {
        //            stream.Position = StartPos + ModelList[i].DataOffset;
        //            int EndPos = 0;
        //            if (i == ModelList.Count - 2)
        //            {
        //                EndPos = ((int)stream.Length - StartPos) - ModelList[i].DataOffset;
        //            }
        //            else
        //            {
        //                EndPos = ModelList[i + 1].DataOffset - ModelList[i].DataOffset;
        //            }

        //            MPFModelHeader modelHandler = ModelList[i];
        //            modelHandler.Matrix = StreamUtil.ReadBytes(stream, EndPos);
        //            RefpackHandler refpackHandler = new RefpackHandler();
        //            modelHandler.Matrix = refpackHandler.Decompress(modelHandler.Matrix);
        //            ModelList[i] = modelHandler;
        //        }
        //    }

        //    //Read Matrix Data
        //    for (int i = 0; i < ModelList.Count - 1; i++)
        //    {
        //        Stream streamMatrix = new MemoryStream();
        //        var Model = ModelList[i];
        //        streamMatrix.Write(ModelList[i].Matrix, 0, ModelList[i].Matrix.Length);

        //        //U7
        //        streamMatrix.Position = Model.U7;
        //        List<MPFUnkownArray1> TempArrayListU7 = new List<MPFUnkownArray1>();
        //        for (int a = 0; a < Model.U17; a++)
        //        {
        //            MPFUnkownArray1 TempArray = new MPFUnkownArray1();
        //            TempArray.Count = StreamUtil.ReadInt32(streamMatrix);
        //            TempArray.StartOffset = StreamUtil.ReadInt32(streamMatrix);
        //            TempArray.EndOffset = StreamUtil.ReadInt32(streamMatrix);
        //            long Position = streamMatrix.Position;

        //            //Read Ints
        //            TempArray.IntList = new List<int>();
        //            streamMatrix.Position = TempArray.StartOffset;
        //            for (int b = 0; b < TempArray.Count; b++)
        //            {
        //                TempArray.IntList.Add(StreamUtil.ReadInt32(streamMatrix));
        //            }
        //            streamMatrix.Position = Position;
        //            TempArrayListU7.Add(TempArray);
        //        }
        //        Model.U7UnkownArray1 = TempArrayListU7;

        //        //U8
        //        streamMatrix.Position = Model.U8;
        //        List<int> ints = new List<int>();
        //        for (int b = 0; b < 27; b++)
        //        {
        //            ints.Add(StreamUtil.ReadInt32(streamMatrix));
        //        }

        //        //U12
        //        streamMatrix.Position = Model.U12;
        //        List<MPFUnkownArray1> TempArrayListU12 = new List<MPFUnkownArray1>();
        //        for (int a = 0; a < 3; a++)
        //        {
        //            MPFUnkownArray1 TempArray = new MPFUnkownArray1();
        //            TempArray.Count = StreamUtil.ReadInt32(streamMatrix);
        //            TempArray.StartOffset = StreamUtil.ReadInt32(streamMatrix);
        //            long Position = streamMatrix.Position;

        //            TempArray.IntList = new List<int>();
        //            streamMatrix.Position = TempArray.StartOffset;
        //            for (int b = 0; b < TempArray.Count; b++)
        //            {
        //                TempArray.IntList.Add(StreamUtil.ReadInt32(streamMatrix));
        //            }
        //            streamMatrix.Position = Position;
        //            TempArrayListU12.Add(TempArray);
        //        }
        //        Model.U12UnkownArray2 = TempArrayListU12;




        //        //Just a translation of whats going on in someone elses program

        //        //Read StripCounter
        //        byte[] tempByte = new byte[] { 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, };
        //        streamMatrix.Position = ByteUtil.FindPosition(streamMatrix, tempByte, 0);
        //        streamMatrix.Position += 47;
        //        int StripCount = StreamUtil.ReadInt32(streamMatrix);
        //        streamMatrix.Position += 12;

        //        List<int> TempStrips = new List<int>();
        //        for (int a = 0; a < StripCount; a++)
        //        {
        //            TempStrips.Add(StreamUtil.ReadInt32(streamMatrix));
        //            streamMatrix.Position += 12;
        //        }
        //        // Incrament the stripcount
        //        List<int> stripCounts2 = new List<int>();
        //        stripCounts2.Add(0);
        //        foreach (var item in TempStrips)
        //        {
        //            stripCounts2.Add(stripCounts2[stripCounts2.Count-1] + item);
        //        }

        //        Model.Strips = stripCounts2;

        //        //Read Vertexes
        //        tempByte = new byte[] { 0x80, 0x3F, 0x00, 0x00, 0x00, 0x20, 0x40, 0x40, 0x40, 0x40, };
        //        streamMatrix.Position = ByteUtil.FindPosition(streamMatrix, tempByte, 0);
        //        streamMatrix.Position += 24;
        //        int VertexCount = StreamUtil.ReadByte(streamMatrix);
        //        streamMatrix.Position++;
        //        List<Vertex3> vertices = new List<Vertex3>();
        //        for (int a = 0; a < VertexCount; a++)
        //        {
        //            Vertex3 vertex = new Vertex3();
        //            vertex.X = StreamUtil.ReadFloat(streamMatrix);
        //            vertex.Y = StreamUtil.ReadFloat(streamMatrix);
        //            vertex.Z = StreamUtil.ReadFloat(streamMatrix);
        //            vertices.Add(vertex);
        //        }
        //        Model.vertices = vertices;

        //        Model.faces = new List<Face>();
        //        int localIndex = 0;
        //        //Make Faces
        //        for (int a = 0; a < Model.vertices.Count; a++)
        //        {
        //            if (InsideSplits(a, Model.Strips))
        //            {
        //                localIndex = 1;
        //                continue;
        //            }
        //            if(localIndex<2)
        //            {
        //                localIndex++;
        //                continue;
        //            }

        //            Model.faces.Add(CreateFaces(a));
        //            localIndex++;
        //        }
        //        ModelList[i] = Model;

        //        streamMatrix.Dispose();
        //        streamMatrix.Close();
        //    }
        //}

        public bool InsideSplits(int Number, List<int> splits)
        {
            foreach (var item in splits)
            {
                if(item==Number)
                {
                    return true;
                }
            }
            return false;
        }

        public Face CreateFaces(int Index)
        {
            Face face = new Face();
            face.V1 = Index;
            face.V2 = Index - 1;
            face.V3 = Index - 2;
            return face;
        }

        public void SaveModel(string path, int pos = 4)
        {
            string output = "";
            var Model = ModelList[pos];
            for (int b = 0; b < Model.models.Count; b++)
            {
                output += "o " + Model.models[b] + b.ToString() + "\n";
                for (int i = 0; i < Model.models[b].vertices.Count; i++)
                {
                    output += "v " + Model.models[b].vertices[i].X + " " + Model.models[b].vertices[i].Y + " " + Model.models[b].vertices[i].Z + "\n";
                }

                for (int i = 0; i < Model.models[b].faces.Count; i++)
                {
                    output += "f " + (Model.models[b].faces[i].V1 + 1).ToString() + " " + (Model.models[b].faces[i].V2 + 1).ToString() + " " + (Model.models[b].faces[i].V3 + 1).ToString() + "\n";
                }
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.WriteAllText(path, output);
        }

        public struct MPFModelHeader
        {
            //GlobalHeader
            public int U1;
            public int SubHeaders;
            public int HeaderSize;
            public int FileStart;
            //Main Header
            public string FileName;
            public int DataOffset;
            public int EntrySize;
            public int ModelsOffset; 
            public int RotationInfo; 
            public int U8;
            public int DataStart;
            //Counts
            public int Chunks;
            public int ModelCount; 
            public int BodyObjectsCount; 
            public int U22;
            public int U23;

            public byte[] Matrix;
            public List<Vertex3> Unkown;
            //Matrix Data
            public List<BodyObjects> bodyObjectsList;
            public List<ModelData> models;
            //
        }

        public struct ModelData
        {
            public string modelName;
            public float Int1;
            public float Float2;
            public float Float3;
            public float Float4;

            public List<Chunk> chunks;

            public List<UV> uv;
            public List<Vertex3> vertices;
            public List<Face> faces;
            public List<int> Strips;
        }

        public struct Chunk
        {
            public int StripCount;
            public int u1;
            public int NormalCount;
            public int VertexCount;
        }


        //public struct MPFModelHeaderSSX3
        //{
        //    //GlobalHeader
        //    public int U1;
        //    public int SubHeaders;
        //    public int HeaderSize;
        //    public int FileStart;
        //    //Main Header
        //    public string ModelName;
        //    public int DataOffset;
        //    public int EntrySize;
        //    public int NameOffset; //Offset Start Of something (Rotation Info?)
        //    public int U7; //Offset Start Of something (After Roation Info?)
        //    public int U8; //Offset Start Of something (Right After U7)
        //    public int U9; //Offset Start Of something 
        //    public int U10; //Blank Guessing Also Offset Start
        //    public int U11; //Same as U7 (After Rotation Rotation Info)
        //    public int U12; //After U7 (After Roation Info)
        //    public int U13;
        //    public int U14;
        //    public int U15;
        //    public int U16;

        //    //Counts
        //    public int U17; //Faces Count? (U7)
        //    public int U18; //!8-20 might be counts related to the bottom
        //    public int U19;
        //    public int U20; //Rotation Info Objects?
        //    public int BodyObjectsCount; //BodyObjects?
        //    public int U22;
        //    public int U23;
        //    public int U24; //VertexCount?

        //    public byte[] Matrix;

        //    public List<BodyObjects> bodyObjectsList;
        //    public List<MPFUnkownArray1> U7UnkownArray1; //Uses U17 As Count
        //    public List<MPFUnkownArray1> U12UnkownArray2;


        //    public List<Vertex3> vertices;
        //    public List<Face> faces;
        //    public List<int> Strips;
        //    //
        //}

        public struct Vertex3
        {
            public float X;
            public float Y;
            public float Z;
        }

        public struct UV
        {
            public int X;
            public int Y;
        }

        public struct Face
        {
            public int V1;
            public int V2;
            public int V3;

            public int UV;
        }

        public struct BodyObjects
        {
            public string Name;
            public float Float1;
            public float Float2;
            public float Float3;
        }

        public struct MPFUnkownArray1
        {
            //Header
            public int Count;
            public int StartOffset; 
            public int EndOffset; //Sometimes Used Sometimes Not
            public List<int> IntList;
        }
    }
}

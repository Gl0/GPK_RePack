﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GPK_RePack.Class;
using GPK_RePack.Class.Prop;
using NLog;
using NLog.Fluent;

namespace GPK_RePack.Parser
{
    class Reader
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public GpkPackage ReadGpk(string path)
        {
            logger.Info("Reading: " + path);

            GpkPackage package = new GpkPackage();

            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
            {
                package.Filename = Path.GetFileName(path);
                package.Path = path;

                ReadHeader(reader, package);
                ReadNames(reader, package);
                ReadImports(reader, package);
                ReadExports(reader, package);
                ReadExportData(reader, package);

                reader.Close();
                reader.Dispose();
            }

            logger.Info("Reading of {0} complete!", path);

            return package;
        }

        private void ReadHeader(BinaryReader reader, GpkPackage package)
        {
            logger.Trace("Header start");

            package.Header.Tag = reader.ReadInt32();
            CheckSignature(package.Header.Tag);

            package.Header.FileVersion = reader.ReadInt16();
            package.Header.LicenseVersion = reader.ReadInt16();

            package.Header.PackageFlags = reader.ReadInt32();

            int len = reader.ReadInt32();
            package.Header.PackageName = ReadString(reader, len);

            package.Header.Unk1 = reader.ReadInt16();
            package.Header.Unk2 = reader.ReadInt16();

            package.Header.NameCount = reader.ReadInt32();
            package.Header.NameOffset = reader.ReadInt32();
            FixNameCount(package);

            package.Header.ExportCount = reader.ReadInt32();
            package.Header.ExportOffset = reader.ReadInt32();

            package.Header.ImportCount = reader.ReadInt32();
            package.Header.ImportOffset = reader.ReadInt32();

            package.Header.DependsOffset = reader.ReadInt32();

            logger.Info("NameCount " + package.Header.NameCount);
            logger.Info("NameOffset " + package.Header.NameOffset);

            logger.Info("ExportCount " + package.Header.ExportCount);
            logger.Info("ExportOffset " + package.Header.ExportOffset);

            logger.Info("ImportCount " + package.Header.ImportCount);
            logger.Info("ImportOffset " + package.Header.ImportOffset);

            logger.Info("DependsOffset " + package.Header.DependsOffset);

            package.Header.FGUID = reader.ReadBytes(16);
            logger.Info("FGUID " + package.Header.FGUID);


            int generation_count = reader.ReadInt32();
            for (int i = 0; i < generation_count; i++)
            {
                GpkGeneration tmpgen = new GpkGeneration();
                tmpgen.ExportCount = reader.ReadInt32();
                tmpgen.NameCount = reader.ReadInt32();
                tmpgen.NetObjectCount = reader.ReadInt32();

                logger.Info("Generation {0}, ExportCount {1}, NameCount {2}, NetObjectCount {3}", i, tmpgen.ExportCount, tmpgen.NameCount, tmpgen.NetObjectCount);

                package.Header.Genrations.Add(tmpgen);
            }


            package.Header.Unk3 = reader.ReadInt32();
            package.Header.Unk4 = reader.ReadInt32();
            package.Header.Unk5 = reader.ReadInt32();
            package.Header.Unk6 = reader.ReadInt32();

            package.Header.EngineVersion = reader.ReadInt32();
            package.Header.CookerVersion = reader.ReadInt32();

            logger.Info("Unk3 {0}, Unk4 {1}, Unk5 {2}, Unk6 {3}, EngineVersion {4}, CookerVersion {5}",
                package.Header.Unk3, package.Header.Unk4, package.Header.Unk5, package.Header.Unk6, package.Header.EngineVersion, package.Header.CookerVersion);
        }

        private void CheckSignature(int sig)
        {
            //A quick validty check
            if (sig != -1641380927) //0x9E2A83C1
            {
                throw new Exception("Not a valid GPK File. Signature Check failed!");
            }
        }

        private void FixNameCount(GpkPackage package)
        {
            int t = package.Header.PackageFlags & 8;
            /*
            if ((package.Header.PackageFlags & 8) == 8)
            {
                package.Header.NameCount -= package.Header.NameOffset;
            }
             */
            package.Header.NameCount -= package.Header.NameOffset;
        }

        private void ReadNames(BinaryReader reader, GpkPackage package)
        {
            logger.Info("Reading Namelist at {0}....", package.Header.NameOffset);
            reader.BaseStream.Seek(package.Header.NameOffset, SeekOrigin.Begin);

            for (int i = 0; i < package.Header.NameCount; i++)
            {
                GpkString tmpString = new GpkString();
                int len = reader.ReadInt32();
                tmpString.name = ReadString(reader, len);
                tmpString.flags = reader.ReadInt64();

                package.NameList.Add(i, tmpString);

                logger.Debug("Name {0}: {1}", i, tmpString.name);
            }

        }

        private void ReadImports(BinaryReader reader, GpkPackage package)
        {
            logger.Info("Reading Imports at {0}....", package.Header.ImportOffset);
            reader.BaseStream.Seek(package.Header.ImportOffset, SeekOrigin.Begin);

            for (int i = 0; i < package.Header.ImportCount; i++)
            {
                GpkImport import = new GpkImport();
                long package_class_index = reader.ReadInt64();
                long class_index = reader.ReadInt64();
                import.PackageRef = reader.ReadInt32();
                long object_index = reader.ReadInt32();
                reader.ReadInt32();

                import.ClassPackage = package.NameList[package_class_index].name;
                import.Class = package.NameList[class_index].name;
                import.Object = package.NameList[object_index].name;



                package.ImportList.Add(i, import);
                logger.Debug("Import {0}: ClassPackage {1} Class: {2} Object: {3}", i, import.ClassPackage, import.Class, import.Object);
            }
        }

        private void ReadExports(BinaryReader reader, GpkPackage package)
        {
            logger.Info("Reading Exports at {0}....", package.Header.ExportOffset);
            reader.BaseStream.Seek(package.Header.ExportOffset, SeekOrigin.Begin);

            for (int i = 0; i < package.Header.ExportCount; i++)
            {
                GpkExport export = new GpkExport();
                export.ClassIndex = reader.ReadInt32();
                export.SuperIndex = reader.ReadInt32();
                export.PackageIndex = reader.ReadInt32();

                long nameIndex = reader.ReadInt32();
                export.Name = package.NameList[nameIndex].name;

                export.Unk1 = reader.ReadInt64();
                export.Unk2 = reader.ReadInt64();

                export.SerialSize = reader.ReadInt32();

                if (export.SerialSize > 0)
                {
                    export.SerialOffset = reader.ReadInt32();
                }
                else
                {
                    logger.Trace(1);
                }

                if (export.ClassIndex < 0)
                {
                    export.ClassIndex = ((export.ClassIndex * -1) - 1);
                    export.ClassName = package.ImportList[export.ClassIndex].Object;
                }

                export.padding_unk = reader.ReadBytes(28);

                package.ExportList.Add(i, export);

                logger.Debug("Export {0}: Class: {1}, Name: {2}, Data_Size: {3}, Data_Offset {4}, Export_offset {5}", i, export.ClassName, export.Name, export.SerialSize, export.SerialOffset, reader.BaseStream.Position);

            }
        }

        private void ReadExportData(BinaryReader reader, GpkPackage package)
        {
            logger.Info("Reading ExportsData....");


            foreach (GpkExport export in package.ExportList.Values)
            {
                reader.BaseStream.Seek(export.SerialOffset, SeekOrigin.Begin);

                //int objectindex (netindex)

                int netindex = reader.ReadInt32();

                //dirty hack until we find the begin 
                while (true)
                {
                    long test_nameindex = reader.ReadInt64();
                    if (package.NameList.ContainsKey(test_nameindex))
                    {
                        reader.BaseStream.Seek(reader.BaseStream.Position - 8, SeekOrigin.Begin);
                        break;
                    }

                    reader.BaseStream.Seek(reader.BaseStream.Position - 7, SeekOrigin.Begin);
                }
                //bad style :(
                //logger.Info("First {0} Skip {1}", first, reader.BaseStream.Position - export.SerialOffset);
                //Props   
                try
                {
                    while (true)
                    {

                        bool cont = ReadPropertyDetails(reader, package, export);
                        if (!cont) break;

                    }

                }
                catch (Exception ex)
                {
                    logger.FatalException("[ReadExportData]", ex);
                }


                if (export.ClassName == "SoundNodeWave")
                {
                    export.data_padding = new byte[32];
                    export.data_padding = reader.ReadBytes(32);
                }

                long object_end = export.SerialOffset + export.SerialSize;
                if (reader.BaseStream.Position < object_end)
                {
                    int toread = (int)(object_end - reader.BaseStream.Position);
                    export.data_start = reader.BaseStream.Position;
                    export.data = new byte[toread];
                    export.data = reader.ReadBytes(toread);
                    logger.Debug(String.Format("Export {0}: Read Data ({1} bytes) and {2} Properties", export.Name, export.data.Length, export.Properties.Count));
                }
                else
                {
                    logger.Debug(String.Format("Export {0}: Read Data (0 bytes) and {1} Properties", export.Name, export.Properties.Count));
                }

                //data

            }
        }
        private Boolean ReadPropertyDetails(BinaryReader reader, GpkPackage package, GpkExport export)
        {
            GpkBaseProperty baseProp = new GpkBaseProperty();

            long nameindex = reader.ReadInt64();
           

            if (!package.NameList.ContainsKey(nameindex))
            {
                logger.Info("name not found");
            }

            baseProp.Name = package.NameList[nameindex].name;
            if (baseProp.Name.ToLower() == "none") return false; 
            
            long typeindex = reader.ReadInt64();
            if (!package.NameList.ContainsKey(typeindex))
            {
                logger.Info("type not found");
            }
            baseProp.type = package.NameList[typeindex].name;

            switch (baseProp.type)
            {
                case "StructProperty":
                /* GpkArrayProperty tmpStruct = new GpkArrayProperty(baseProp);
                 long structname = reader.ReadInt64();
                 long structtype = reader.ReadInt64();
                 string tmpStrucName = package.NameList[structname].name;
                 string tmpType = package.NameList[structtype].name;
                 switch (tmpStrucName)
                 {
                     case "lol":
                         break;
                     default:
                         logger.Trace("fu");
                         break;
                 }
                        

                 tmpStruct.value = new byte[tmpStruct.length];
                 tmpStruct.value = reader.ReadBytes((int)tmpStruct.length);
                   
                    

                 export.Properties.Add(tmpStruct);
                 break; 
                 */
                case "ArrayProperty":
                    GpkArrayProperty tmpArray = new GpkArrayProperty(baseProp);
                    tmpArray.length = reader.ReadInt64();
                    tmpArray.value = new byte[tmpArray.length];
                    tmpArray.value = reader.ReadBytes((int)tmpArray.length);

                    export.Properties.Add(tmpArray);
                    break;

                case " ": //DiffuseColor
                    GpkGUIDProperty tmpGUID = new GpkGUIDProperty(baseProp);
                    tmpGUID.length = reader.ReadInt64();
                    tmpGUID.unk = reader.ReadInt64();
                    tmpGUID.value = new byte[tmpGUID.length];
                    tmpGUID.value = reader.ReadBytes((int)tmpGUID.length);

                    export.Properties.Add(tmpGUID);
                    break;

                case "BoolProperty":
                    GpkBoolProperty tmpBool = new GpkBoolProperty(baseProp);
                    tmpBool.unk = reader.ReadInt64();
                    tmpBool.value = Convert.ToBoolean(reader.ReadInt32());
                    export.Properties.Add(tmpBool);
                    break;

                case "NameProperty":
                case "ByteProperty":
                    GpkNameProperty tmpName = new GpkNameProperty(baseProp);
                    tmpName.unk = reader.ReadInt64();
                    long index = reader.ReadInt64();
                    tmpName.value = package.NameList[index].name;

                    export.Properties.Add(tmpName);
                    break;

                case "IntProperty":
                    GpkIntProperty tmpInt = new GpkIntProperty(baseProp);
                    tmpInt.unk = reader.ReadInt64();
                    tmpInt.value = reader.ReadInt32();

                    export.Properties.Add(tmpInt);
                    break;


                case "StrProperty":
                    GpkStringProperty tmpString = new GpkStringProperty(baseProp);
                    tmpString.unk = reader.ReadInt64(); //outer len
                    tmpString.length = reader.ReadInt32(); //inner len

                    if (tmpString.length > 0)
                    {
                        tmpString.value = ReadString(reader, (int)tmpString.length);
                    }
                    else
                    {
                        //unicode :O
                        tmpString.value = ReadUnicodeString(reader, (int)tmpString.unk);
                    }


                    export.Properties.Add(tmpString);
                    break;

                case "ObjectProperty":
                    GpkObjectProperty tmpObj = new GpkObjectProperty(baseProp);
                    tmpObj.unk = reader.ReadInt64(); //4
                    tmpObj.value = reader.ReadInt32();

                    if (tmpObj.value < 0)
                    {
                        tmpObj.ClassName = package.ImportList[((tmpObj.value * -1) - 1)].Object;
                    }
                    else if (tmpObj.value > 0)
                    {
                        tmpObj.ClassName = package.ExportList[tmpObj.value].Name;
                    }
                    else if (tmpObj.value == 0)
                    {
                        tmpObj.ClassName = null;
                    }

                    export.Properties.Add(tmpObj);
                    break;

                case "FloatProperty":
                    GpkFloatProperty tmpFloat = new GpkFloatProperty(baseProp);
                    tmpFloat.unk = reader.ReadInt64();
                    tmpFloat.value = reader.ReadSingle();

                    export.Properties.Add(tmpFloat);
                    break;
                default:
                    throw new Exception(
                        string.Format(
                            "Unknown Property Type {0}, Position {1}, Prop_Name {2}, Export_Name {3}",
                            baseProp.type, reader.BaseStream.Position, baseProp.Name, export.Name));




            }

            logger.Trace(String.Format("Property Type {0}, Position after {1}, Prop_Name {2}, Export_Name {3}",
                            baseProp.type, reader.BaseStream.Position, baseProp.Name, export.Name));

            return true;
        }
        private string ReadString(BinaryReader reader, int length)
        {
            string text = ASCIIEncoding.ASCII.GetString(reader.ReadBytes(length - 1));
            reader.ReadByte(); //text control char 

            return text;
        }

        private string ReadUnicodeString(BinaryReader reader, int length)
        {
            string text = UnicodeEncoding.Unicode.GetString(reader.ReadBytes(length - 6));
            reader.ReadBytes(2); //text control char 

            return text;
        }


    }
}
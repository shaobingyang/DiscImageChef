﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : ATA.cs
// Version        : 1.0
// Author(s)      : Natalia Portillo
//
// Component      : Component
//
// Revision       : $Revision$
// Last change by : $Author$
// Date           : $Date$
//
// --[ Description ] ----------------------------------------------------------
//
// Description
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright (C) 2011-2015 Claunia.com
// ****************************************************************************/
// //$Id$
using System;
using System.Collections.Generic;
using System.IO;
using DiscImageChef.CommonTypes;
using DiscImageChef.Console;
using DiscImageChef.Core.Logging;
using DiscImageChef.Decoders.PCMCIA;
using DiscImageChef.Devices;
using DiscImageChef.Filesystems;
using DiscImageChef.Filters;
using DiscImageChef.ImagePlugins;
using DiscImageChef.PartPlugins;
using Schemas;

namespace DiscImageChef.Core.Devices.Dumping
{
    public class ATA
    {
        public static void Dump(Device dev, string devicePath, string outputPrefix, ushort retryPasses, bool force, bool dumpRaw, bool persistent, bool stopOnError)
        {
            bool aborted;
            MHDDLog mhddLog;
            IBGLog ibgLog;

            if(dumpRaw)
            {
                DicConsole.ErrorWriteLine("Raw dumping not yet supported in ATA devices.");

                if(force)
                    DicConsole.ErrorWriteLine("Continuing...");
                else
                {
                    DicConsole.ErrorWriteLine("Aborting...");
                    return;
                }
            }

            byte[] cmdBuf;
            bool sense;
            ushort currentProfile = 0x0001;
            Decoders.ATA.AtaErrorRegistersCHS errorChs;
            uint timeout = 5;
            double duration;

            sense = dev.AtaIdentify(out cmdBuf, out errorChs);
            if(!sense && Decoders.ATA.Identify.Decode(cmdBuf).HasValue)
            {
                Decoders.ATA.Identify.IdentifyDevice ataId = Decoders.ATA.Identify.Decode(cmdBuf).Value;

                CICMMetadataType sidecar = new CICMMetadataType();
                sidecar.BlockMedia = new BlockMediaType[1];
                sidecar.BlockMedia[0] = new BlockMediaType();

                if(dev.IsUSB)
                {
                    sidecar.BlockMedia[0].USB = new USBType();
                    sidecar.BlockMedia[0].USB.ProductID = dev.USBProductID;
                    sidecar.BlockMedia[0].USB.VendorID = dev.USBVendorID;
                    sidecar.BlockMedia[0].USB.Descriptors = new DumpType();
                    sidecar.BlockMedia[0].USB.Descriptors.Image = outputPrefix + ".usbdescriptors.bin";
                    sidecar.BlockMedia[0].USB.Descriptors.Size = dev.USBDescriptors.Length;
                    sidecar.BlockMedia[0].USB.Descriptors.Checksums = Checksum.GetChecksums(dev.USBDescriptors).ToArray();
                    DataFile.WriteTo("ATA Dump", sidecar.BlockMedia[0].USB.Descriptors.Image, dev.USBDescriptors);
                }

                if(dev.IsPCMCIA)
                {
                    sidecar.BlockMedia[0].PCMCIA = new PCMCIAType();
                    sidecar.BlockMedia[0].PCMCIA.CIS = new DumpType();
                    sidecar.BlockMedia[0].PCMCIA.CIS.Image = outputPrefix + ".cis.bin";
                    sidecar.BlockMedia[0].PCMCIA.CIS.Size = dev.CIS.Length;
                    sidecar.BlockMedia[0].PCMCIA.CIS.Checksums = Checksum.GetChecksums(dev.CIS).ToArray();
                    DataFile.WriteTo("ATA Dump", sidecar.BlockMedia[0].PCMCIA.CIS.Image, dev.CIS);
                    Decoders.PCMCIA.Tuple[] tuples = CIS.GetTuples(dev.CIS);
                    if(tuples != null)
                    {
                        foreach(Decoders.PCMCIA.Tuple tuple in tuples)
                        {
                            if(tuple.Code == TupleCodes.CISTPL_MANFID)
                            {
                                ManufacturerIdentificationTuple manfid = CIS.DecodeManufacturerIdentificationTuple(tuple);

                                if(manfid != null)
                                {
                                    sidecar.BlockMedia[0].PCMCIA.ManufacturerCode = manfid.ManufacturerID;
                                    sidecar.BlockMedia[0].PCMCIA.CardCode = manfid.CardID;
                                    sidecar.BlockMedia[0].PCMCIA.ManufacturerCodeSpecified = true;
                                    sidecar.BlockMedia[0].PCMCIA.CardCodeSpecified = true;
                                }
                            }
                            else if(tuple.Code == TupleCodes.CISTPL_VERS_1)
                            {
                                Level1VersionTuple vers = CIS.DecodeLevel1VersionTuple(tuple);

                                if(vers != null)
                                {
                                    sidecar.BlockMedia[0].PCMCIA.Manufacturer = vers.Manufacturer;
                                    sidecar.BlockMedia[0].PCMCIA.ProductName = vers.Product;
                                    sidecar.BlockMedia[0].PCMCIA.Compliance = string.Format("{0}.{1}", vers.MajorVersion, vers.MinorVersion);
                                    sidecar.BlockMedia[0].PCMCIA.AdditionalInformation = vers.AdditionalInformation;
                                }
                            }
                        }
                    }
                }

                sidecar.BlockMedia[0].ATA = new ATAType();
                sidecar.BlockMedia[0].ATA.Identify = new DumpType();
                sidecar.BlockMedia[0].ATA.Identify.Image = outputPrefix + ".identify.bin";
                sidecar.BlockMedia[0].ATA.Identify.Size = cmdBuf.Length;
                sidecar.BlockMedia[0].ATA.Identify.Checksums = Checksum.GetChecksums(cmdBuf).ToArray();
                DataFile.WriteTo("ATA Dump", sidecar.BlockMedia[0].ATA.Identify.Image, cmdBuf);

                DateTime start;
                DateTime end;
                double totalDuration = 0;
                double totalChkDuration = 0;
                double currentSpeed = 0;
                double maxSpeed = double.MinValue;
                double minSpeed = double.MaxValue;
                List<ulong> unreadableSectors = new List<ulong>();
                Checksum dataChk;

                aborted = false;
                System.Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = aborted = true;
                };

                DataFile dumpFile;

                // Initializate reader
                Reader ataReader = new Reader(dev, timeout, cmdBuf);
                // Fill reader blocks
                ulong blocks = ataReader.GetDeviceBlocks();
                // Check block sizes
                if(ataReader.GetBlockSize())
                {
                    DicConsole.ErrorWriteLine(ataReader.ErrorMessage);
                    return;
                }
                uint blockSize = ataReader.LogicalBlockSize;
                uint physicalsectorsize = ataReader.PhysicalBlockSize;
                if(ataReader.FindReadCommand())
                {
                    DicConsole.ErrorWriteLine(ataReader.ErrorMessage);
                    return;
                }
                // Check how many blocks to read, if error show and return
                if(ataReader.GetBlocksToRead())
                {
                    DicConsole.ErrorWriteLine(ataReader.ErrorMessage);
                    return;
                }
                uint blocksToRead = ataReader.BlocksToRead;
                ushort cylinders = ataReader.Cylinders;
                byte heads = ataReader.Heads;
                byte sectors = ataReader.Sectors;

                if(ataReader.IsLBA)
                {
                    DicConsole.WriteLine("Reading {0} sectors at a time.", blocksToRead);

                    mhddLog = new MHDDLog(outputPrefix + ".mhddlog.bin", dev, blocks, blockSize, blocksToRead);
                    ibgLog = new IBGLog(outputPrefix + ".ibg", currentProfile);
                    dumpFile = new DataFile(outputPrefix + ".bin");

                    start = DateTime.UtcNow;
                    for(ulong i = 0; i < blocks; i += blocksToRead)
                    {
                        if(aborted)
                            break;

                        if((blocks - i) < blocksToRead)
                            blocksToRead = (byte)(blocks - i);

#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
                        if(currentSpeed > maxSpeed && currentSpeed != 0)
                            maxSpeed = currentSpeed;
                        if(currentSpeed < minSpeed && currentSpeed != 0)
                            minSpeed = currentSpeed;
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator

                        DicConsole.Write("\rReading sector {0} of {1} ({2:F3} MiB/sec.)", i, blocks, currentSpeed);

                        bool error = ataReader.ReadBlocks(out cmdBuf, i, blocksToRead, out duration);

                        if(!error)
                        {
                            mhddLog.Write(i, duration);
                            ibgLog.Write(i, currentSpeed * 1024);
                            dumpFile.Write(cmdBuf);
                        }
                        else
                        {
                            unreadableSectors.Add(i);
                            if(duration < 500)
                                mhddLog.Write(i, 65535);
                            else
                                mhddLog.Write(i, duration);

                            ibgLog.Write(i, 0);
                            dumpFile.Write(new byte[blockSize * blocksToRead]);
                        }

#pragma warning disable IDE0004 // Cast is necessary, otherwise incorrect value is created
                        currentSpeed = ((double)blockSize * blocksToRead / (double)1048576) / (duration / (double)1000);
#pragma warning restore IDE0004 // Cast is necessary, otherwise incorrect value is created
                        GC.Collect();
                    }
                    end = DateTime.Now;
                    DicConsole.WriteLine();
                    mhddLog.Close();
#pragma warning disable IDE0004 // Cast is necessary, otherwise incorrect value is created
                    ibgLog.Close(dev, blocks, blockSize, (end - start).TotalSeconds, currentSpeed * 1024, (((double)blockSize * (double)(blocks + 1)) / 1024) / (totalDuration / 1000), devicePath);
#pragma warning restore IDE0004 // Cast is necessary, otherwise incorrect value is created

                    #region Error handling
                    if(unreadableSectors.Count > 0 && !aborted)
                    {
                        List<ulong> tmpList = new List<ulong>();

                        foreach(ulong ur in unreadableSectors)
                        {
                            for(ulong i = ur; i < ur + blocksToRead; i++)
                                tmpList.Add(i);
                        }

                        tmpList.Sort();

                        int pass = 0;
                        bool forward = true;
                        bool runningPersistent = false;

                        unreadableSectors = tmpList;

                    repeatRetryLba:
                        ulong[] tmpArray = unreadableSectors.ToArray();
                        foreach(ulong badSector in tmpArray)
                        {
                            if(aborted)
                                break;

                            DicConsole.Write("\rRetrying sector {0}, pass {1}, {3}{2}", badSector, pass + 1, forward ? "forward" : "reverse", runningPersistent ? "recovering partial data, " : "");

                            bool error = ataReader.ReadBlock(out cmdBuf, badSector, out duration);

                            totalDuration += duration;

                            if(!error)
                            {
                                unreadableSectors.Remove(badSector);
                                dumpFile.WriteAt(cmdBuf, badSector, blockSize);
                            }
                            else if(runningPersistent)
                                dumpFile.WriteAt(cmdBuf, badSector, blockSize);
                        }

                        if(pass < retryPasses && !aborted && unreadableSectors.Count > 0)
                        {
                            pass++;
                            forward = !forward;
                            unreadableSectors.Sort();
                            unreadableSectors.Reverse();
                            goto repeatRetryLba;
                        }

                        DicConsole.WriteLine();
                    }
                    #endregion Error handling LBA
                }
                else
                {
                    mhddLog = new MHDDLog(outputPrefix + ".mhddlog.bin", dev, blocks, blockSize, blocksToRead);
                    ibgLog = new IBGLog(outputPrefix + ".ibg", currentProfile);
                    dumpFile = new DataFile(outputPrefix + ".bin");

                    ulong currentBlock = 0;
                    blocks = (ulong)(cylinders * heads * sectors);
                    start = DateTime.UtcNow;
                    for(ushort Cy = 0; Cy < cylinders; Cy++)
                    {
                        for(byte Hd = 0; Hd < heads; Hd++)
                        {
                            for(byte Sc = 1; Sc < sectors; Sc++)
                            {
                                if(aborted)
                                    break;

#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
                                if(currentSpeed > maxSpeed && currentSpeed != 0)
                                    maxSpeed = currentSpeed;
                                if(currentSpeed < minSpeed && currentSpeed != 0)
                                    minSpeed = currentSpeed;
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator

                                DicConsole.Write("\rReading cylinder {0} head {1} sector {2} ({3:F3} MiB/sec.)", Cy, Hd, Sc, currentSpeed);

                                bool error = ataReader.ReadCHS(out cmdBuf, Cy, Hd, Sc, out duration);

                                totalDuration += duration;

                                if(!error)
                                {
                                    mhddLog.Write(currentBlock, duration);
                                    ibgLog.Write(currentBlock, currentSpeed * 1024);
                                    dumpFile.Write(cmdBuf);
                                }
                                else
                                {
                                    unreadableSectors.Add(currentBlock);
                                    if(duration < 500)
                                        mhddLog.Write(currentBlock, 65535);
                                    else
                                        mhddLog.Write(currentBlock, duration);

                                    ibgLog.Write(currentBlock, 0);
                                    dumpFile.Write(new byte[blockSize]);
                                }

#pragma warning disable IDE0004 // Cast is necessary, otherwise incorrect value is created
                                currentSpeed = ((double)blockSize / (double)1048576) / (duration / (double)1000);
#pragma warning disable IDE0004 // Cast is necessary, otherwise incorrect value is created
                                GC.Collect();

                                currentBlock++;
                            }
                        }
                    }
                    end = DateTime.Now;
                    DicConsole.WriteLine();
                    mhddLog.Close();
#pragma warning disable IDE0004 // Cast is necessary, otherwise incorrect value is created
                    ibgLog.Close(dev, blocks, blockSize, (end - start).TotalSeconds, currentSpeed * 1024, (((double)blockSize * (double)(blocks + 1)) / 1024) / (totalDuration / 1000), devicePath);
#pragma warning disable IDE0004 // Cast is necessary, otherwise incorrect value is created
                }
                dataChk = new Checksum();
                dumpFile.Seek(0, SeekOrigin.Begin);
                blocksToRead = 500;

                for(ulong i = 0; i < blocks; i += blocksToRead)
                {
                    if(aborted)
                        break;

                    if((blocks - i) < blocksToRead)
                        blocksToRead = (byte)(blocks - i);

                    DicConsole.Write("\rChecksumming sector {0} of {1} ({2:F3} MiB/sec.)", i, blocks, currentSpeed);

                    DateTime chkStart = DateTime.UtcNow;
                    byte[] dataToCheck = new byte[blockSize * blocksToRead];
                    dumpFile.Read(dataToCheck, 0, (int)(blockSize * blocksToRead));
                    dataChk.Update(dataToCheck);
                    DateTime chkEnd = DateTime.UtcNow;

                    double chkDuration = (chkEnd - chkStart).TotalMilliseconds;
                    totalChkDuration += chkDuration;

                    currentSpeed = ((double)blockSize * blocksToRead / (double)1048576) / (chkDuration / (double)1000);
                }
                DicConsole.WriteLine();
                dumpFile.Close();
                end = DateTime.UtcNow;

                PluginBase plugins = new PluginBase();
                plugins.RegisterAllPlugins();
                ImagePlugin _imageFormat;

                FiltersList filtersList = new FiltersList();
                Filter inputFilter = filtersList.GetFilter(outputPrefix + ".bin");

                if(inputFilter == null)
                {
                    DicConsole.ErrorWriteLine("Cannot open file just created, this should not happen.");
                    return;
                }

                _imageFormat = ImageFormat.Detect(inputFilter);
                PartitionType[] xmlFileSysInfo = null;

                try
                {
                    if(!_imageFormat.OpenImage(inputFilter))
                        _imageFormat = null;
                }
                catch
                {
                    _imageFormat = null;
                }

                if(_imageFormat != null)
                {
                    List<Partition> partitions = new List<Partition>();

                    foreach(PartPlugin _partplugin in plugins.PartPluginsList.Values)
                    {
                        List<Partition> _partitions;

                        if(_partplugin.GetInformation(_imageFormat, out _partitions))
                        {
                            partitions.AddRange(_partitions);
                            Statistics.AddPartition(_partplugin.Name);
                        }
                    }

                    if(partitions.Count > 0)
                    {
                        xmlFileSysInfo = new PartitionType[partitions.Count];
                        for(int i = 0; i < partitions.Count; i++)
                        {
                            xmlFileSysInfo[i] = new PartitionType();
                            xmlFileSysInfo[i].Description = partitions[i].PartitionDescription;
                            xmlFileSysInfo[i].EndSector = (int)(partitions[i].PartitionStartSector + partitions[i].PartitionSectors - 1);
                            xmlFileSysInfo[i].Name = partitions[i].PartitionName;
                            xmlFileSysInfo[i].Sequence = (int)partitions[i].PartitionSequence;
                            xmlFileSysInfo[i].StartSector = (int)partitions[i].PartitionStartSector;
                            xmlFileSysInfo[i].Type = partitions[i].PartitionType;

                            List<FileSystemType> lstFs = new List<FileSystemType>();

                            foreach(Filesystem _plugin in plugins.PluginsList.Values)
                            {
                                try
                                {
                                    if(_plugin.Identify(_imageFormat, partitions[i].PartitionStartSector, partitions[i].PartitionStartSector + partitions[i].PartitionSectors - 1))
                                    {
                                        string foo;
                                        _plugin.GetInformation(_imageFormat, partitions[i].PartitionStartSector, partitions[i].PartitionStartSector + partitions[i].PartitionSectors - 1, out foo);
                                        lstFs.Add(_plugin.XmlFSType);
                                        Statistics.AddFilesystem(_plugin.XmlFSType.Type);
                                    }
                                }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                                catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                                {
                                    //DicConsole.DebugWriteLine("Dump-media command", "Plugin {0} crashed", _plugin.Name);
                                }
                            }

                            if(lstFs.Count > 0)
                                xmlFileSysInfo[i].FileSystems = lstFs.ToArray();
                        }
                    }
                    else
                    {
                        xmlFileSysInfo = new PartitionType[1];
                        xmlFileSysInfo[0] = new PartitionType();
                        xmlFileSysInfo[0].EndSector = (int)(blocks - 1);
                        xmlFileSysInfo[0].StartSector = 0;

                        List<FileSystemType> lstFs = new List<FileSystemType>();

                        foreach(Filesystem _plugin in plugins.PluginsList.Values)
                        {
                            try
                            {
                                if(_plugin.Identify(_imageFormat, (blocks - 1), 0))
                                {
                                    string foo;
                                    _plugin.GetInformation(_imageFormat, (blocks - 1), 0, out foo);
                                    lstFs.Add(_plugin.XmlFSType);
                                    Statistics.AddFilesystem(_plugin.XmlFSType.Type);
                                }
                            }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                            catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                            {
                                //DicConsole.DebugWriteLine("Create-sidecar command", "Plugin {0} crashed", _plugin.Name);
                            }
                        }

                        if(lstFs.Count > 0)
                            xmlFileSysInfo[0].FileSystems = lstFs.ToArray();
                    }
                }

                sidecar.BlockMedia[0].Checksums = dataChk.End().ToArray();
                string xmlDskTyp, xmlDskSubTyp;
                if(dev.IsCompactFlash)
                    Metadata.MediaType.MediaTypeToString(MediaType.CompactFlash, out xmlDskTyp, out xmlDskSubTyp);
                else if(dev.IsPCMCIA)
                    Metadata.MediaType.MediaTypeToString(MediaType.PCCardTypeI, out xmlDskTyp, out xmlDskSubTyp);
                else
                    Metadata.MediaType.MediaTypeToString(MediaType.GENERIC_HDD, out xmlDskTyp, out xmlDskSubTyp);
                sidecar.BlockMedia[0].DiskType = xmlDskTyp;
                sidecar.BlockMedia[0].DiskSubType = xmlDskSubTyp;
                // TODO: Implement device firmware revision
                sidecar.BlockMedia[0].Image = new ImageType();
                sidecar.BlockMedia[0].Image.format = "Raw disk image (sector by sector copy)";
                sidecar.BlockMedia[0].Image.Value = outputPrefix + ".bin";
                sidecar.BlockMedia[0].Interface = "ATA";
                sidecar.BlockMedia[0].LogicalBlocks = (long)blocks;
                sidecar.BlockMedia[0].PhysicalBlockSize = (int)physicalsectorsize;
                sidecar.BlockMedia[0].LogicalBlockSize = (int)blockSize;
                sidecar.BlockMedia[0].Manufacturer = dev.Manufacturer;
                sidecar.BlockMedia[0].Model = dev.Model;
                sidecar.BlockMedia[0].Serial = dev.Serial;
                sidecar.BlockMedia[0].Size = (long)(blocks * blockSize);
                if(xmlFileSysInfo != null)
                    sidecar.BlockMedia[0].FileSystemInformation = xmlFileSysInfo;
                if(cylinders > 0 && heads > 0 && sectors > 0)
                {
                    sidecar.BlockMedia[0].Cylinders = cylinders;
                    sidecar.BlockMedia[0].CylindersSpecified = true;
                    sidecar.BlockMedia[0].Heads = heads;
                    sidecar.BlockMedia[0].HeadsSpecified = true;
                    sidecar.BlockMedia[0].SectorsPerTrack = sectors;
                    sidecar.BlockMedia[0].SectorsPerTrackSpecified = true;
                }

                DicConsole.WriteLine();

                DicConsole.WriteLine("Took a total of {0:F3} seconds ({1:F3} processing commands, {2:F3} checksumming).", (end - start).TotalSeconds, totalDuration / 1000, totalChkDuration / 1000);
                DicConsole.WriteLine("Avegare speed: {0:F3} MiB/sec.", (((double)blockSize * (double)(blocks + 1)) / 1048576) / (totalDuration / 1000));
                DicConsole.WriteLine("Fastest speed burst: {0:F3} MiB/sec.", maxSpeed);
                DicConsole.WriteLine("Slowest speed burst: {0:F3} MiB/sec.", minSpeed);
                DicConsole.WriteLine("{0} sectors could not be read.", unreadableSectors.Count);
                if(unreadableSectors.Count > 0)
                {
                    unreadableSectors.Sort();
                    foreach(ulong bad in unreadableSectors)
                        DicConsole.WriteLine("Sector {0} could not be read", bad);
                }
                DicConsole.WriteLine();

                if(!aborted)
                {
                    DicConsole.WriteLine("Writing metadata sidecar");

                    FileStream xmlFs = new FileStream(outputPrefix + ".cicm.xml",
                                                  FileMode.Create);

                    System.Xml.Serialization.XmlSerializer xmlSer = new System.Xml.Serialization.XmlSerializer(typeof(CICMMetadataType));
                    xmlSer.Serialize(xmlFs, sidecar);
                    xmlFs.Close();
                }

                Statistics.AddMedia(MediaType.GENERIC_HDD, true);
            }
            else
                DicConsole.ErrorWriteLine("Unable to communicate with ATA device.");
        }
    }
}
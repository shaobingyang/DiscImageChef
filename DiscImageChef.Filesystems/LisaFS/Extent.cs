﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Extent.cs
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
using DiscImageChef.Console;
using DiscImageChef.ImagePlugins;

namespace DiscImageChef.Filesystems.LisaFS
{
    partial class LisaFS : Filesystem
    {
        public override Errno MapBlock(string path, long fileBlock, ref long deviceBlock)
        {
            // TODO: Not really important.
            return Errno.NotImplemented;
        }

        /// <summary>
        /// Searches the disk for an extents file (or gets it from cache)
        /// </summary>
        /// <returns>Error.</returns>
        /// <param name="fileId">File identifier.</param>
        /// <param name="file">Extents file.</param>
        Errno ReadExtentsFile(Int16 fileId, out ExtentFile file)
        {
            file = new ExtentFile();

            if(!mounted)
                return Errno.AccessDenied;

            if(fileId < 5)
                return Errno.InvalidArgument;

            if(extentCache.TryGetValue(fileId, out file))
                return Errno.NoError;

            // If the file is found but not its extents file we should suppose it's a directory
            bool fileFound = false;

            for(ulong i = 0; i < device.GetSectors(); i++)
            {
                byte[] tag = device.ReadSectorTag((ulong)i, SectorTagType.AppleSectorTag);
                Int16 foundid = BigEndianBitConverter.ToInt16(tag, 0x04);

                if(foundid == fileId)
                    fileFound = true;

                if(foundid == ((short)(-1 * fileId)))
                {
                    byte[] sector = device.ReadSector((ulong)i);

                    if(sector[0] >= 32 || sector[0] == 0)
                        return Errno.InvalidArgument;

                    file.filenameLen = sector[0];
                    file.filename = new byte[file.filenameLen];
                    Array.Copy(sector, 0x01, file.filename, 0, file.filenameLen);
                    file.unknown1 = BigEndianBitConverter.ToUInt16(sector, 0x20);
                    file.file_uid = BigEndianBitConverter.ToUInt64(sector, 0x22);
                    file.unknown2 = sector[0x2A];
                    file.etype = sector[0x2B];
                    file.ftype = (FileType)sector[0x2C];
                    file.unknown3 = sector[0x2D];
                    file.dtc = BigEndianBitConverter.ToUInt32(sector, 0x2E);
                    file.dta = BigEndianBitConverter.ToUInt32(sector, 0x32);
                    file.dtm = BigEndianBitConverter.ToUInt32(sector, 0x36);
                    file.dtb = BigEndianBitConverter.ToUInt32(sector, 0x3A);
                    file.dts = BigEndianBitConverter.ToUInt32(sector, 0x3E);
                    file.serial = BigEndianBitConverter.ToUInt32(sector, 0x42);
                    file.unknown4 = sector[0x46];
                    file.locked = sector[0x47];
                    file.protect = sector[0x48];
                    file.master = sector[0x49];
                    file.scavenged = sector[0x4A];
                    file.closed = sector[0x4B];
                    file.open = sector[0x4C];
                    file.unknown5 = new byte[11];
                    Array.Copy(sector, 0x4D, file.unknown5, 0, 11);
                    file.release = BigEndianBitConverter.ToUInt16(sector, 0x58);
                    file.build = BigEndianBitConverter.ToUInt16(sector, 0x5A);
                    file.compatibility = BigEndianBitConverter.ToUInt16(sector, 0x5C);
                    file.revision = BigEndianBitConverter.ToUInt16(sector, 0x5E);
                    file.unknown6 = BigEndianBitConverter.ToUInt16(sector, 0x60);
                    file.password_valid = sector[0x62];
                    file.password = new byte[8];
                    Array.Copy(sector, 0x63, file.password, 0, 8);
                    file.unknown7 = new byte[3];
                    Array.Copy(sector, 0x6B, file.unknown7, 0, 3);
                    file.overhead = BigEndianBitConverter.ToUInt16(sector, 0x6E);
                    file.unknown8 = new byte[16];
                    Array.Copy(sector, 0x70, file.unknown8, 0, 16);
                    file.length = BigEndianBitConverter.ToInt32(sector, 0x80);
                    file.unknown9 = BigEndianBitConverter.ToInt32(sector, 0x84);
                    file.unknown10 = BigEndianBitConverter.ToInt16(sector, 0x17E);
                    file.LisaInfo = new byte[128];
                    Array.Copy(sector, 0x180, file.LisaInfo, 0, 128);

                    int extentsCount = 0;

                    for(int j = 0; j < 41; j++)
                    {
                        if(BigEndianBitConverter.ToInt16(sector, 0x88 + j * 6 + 4) == 0)
                            break;

                        extentsCount++;
                    }

                    file.extents = new Extent[extentsCount];

                    for(int j = 0; j < extentsCount; j++)
                    {
                        file.extents[j] = new Extent();
                        file.extents[j].start = BigEndianBitConverter.ToInt32(sector, 0x88 + j * 6);
                        file.extents[j].length = BigEndianBitConverter.ToInt16(sector, 0x88 + j * 6 + 4);
                    }

                    extentCache.Add(fileId, file);

                    if(debug)
                    {
                        if(!printedExtents.Contains(fileId))
                        {
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].filenameLen = {1}", fileId, file.filenameLen);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].filename = {1}", fileId, StringHandlers.CToString(file.filename));
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].unknown1 = 0x{1:X4}", fileId, file.unknown1);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].file_uid = 0x{1:X16}", fileId, file.file_uid);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].unknown2 = 0x{1:X2}", fileId, file.unknown2);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].etype = 0x{1:X2}", fileId, file.etype);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].ftype = {1}", fileId, file.ftype);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].unknown3 = 0x{1:X2}", fileId, file.unknown3);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].dtc = {1}", fileId, file.dtc);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].dta = {1}", fileId, file.dta);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].dtm = {1}", fileId, file.dtm);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].dtb = {1}", fileId, file.dtb);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].dts = {1}", fileId, file.dts);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].serial = {1}", fileId, file.serial);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].unknown4 = 0x{1:X2}", fileId, file.unknown4);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].locked = {1}", fileId, file.locked > 0);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].protect = {1}", fileId, file.protect > 0);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].master = {1}", fileId, file.master > 0);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].scavenged = {1}", fileId, file.scavenged > 0);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].closed = {1}", fileId, file.closed > 0);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].open = {1}", fileId, file.open > 0);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].unknown5 = 0x{1:X2}{2:X2}{3:X2}{4:X2}{5:X2}{6:X2}{7:X2}{8:X2}{9:X2}" +
                                                      "{10:X2}{11:X2}", fileId, file.unknown5[0], file.unknown5[1], file.unknown5[2], file.unknown5[3],
                                                      file.unknown5[4], file.unknown5[5], file.unknown5[6], file.unknown5[7], file.unknown5[8], file.unknown5[9],
                                                      file.unknown5[10]);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].release = {1}", fileId, file.release);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].build = {1}", fileId, file.build);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].compatibility = {1}", fileId, file.compatibility);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].revision = {1}", fileId, file.revision);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].unknown6 = 0x{1:X4}", fileId, file.unknown6);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].password_valid = {1}", fileId, file.password_valid > 0);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].password = {1}", fileId, StringHandlers.CToString(file.password));
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].unknown7 = 0x{1:X2}{2:X2}{3:X2}", fileId, file.unknown7[0],
                                                      file.unknown7[1], file.unknown7[2]);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].overhead = {1}", fileId, file.overhead);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].unknown8 = 0x{1:X2}{2:X2}{3:X2}{4:X2}{5:X2}{6:X2}{7:X2}{8:X2}{9:X2}" +
                                                      "{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}{16:X2}", fileId, file.unknown8[0], file.unknown8[1], file.unknown8[2],
                                                      file.unknown8[3], file.unknown8[4], file.unknown8[5], file.unknown8[6], file.unknown8[7], file.unknown8[8],
                                                      file.unknown8[9], file.unknown8[10], file.unknown8[11], file.unknown8[12], file.unknown8[13], file.unknown8[14],
                                                      file.unknown8[15]);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].length = {1}", fileId, file.length);
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].unknown9 = 0x{1:X8}", fileId, file.unknown9);
                            for(int ext = 0; ext < file.extents.Length; ext++)
                            {
                                DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].extents[{1}].start = {2}", fileId, ext, file.extents[ext].start);
                                DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].extents[{1}].length = {2}", fileId, ext, file.extents[ext].length);
                            }
                            DicConsole.DebugWriteLine("LisaFS plugin", "ExtentFile[{0}].unknown10 = 0x{1:X4}", fileId, file.unknown10);

                            printedExtents.Add(fileId);
                        }
                    }

                    return Errno.NoError;
                }
            }

            return fileFound ? Errno.IsDirectory : Errno.NoSuchFile;
        }
    }
}

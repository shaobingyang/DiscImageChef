﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : HL-DT-ST.cs
// Version        : 1.0
// Author(s)      : Natalia Portillo
//
// Component      : HL-DT-ST vendor commands
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

namespace DiscImageChef.Devices
{
    public partial class Device
    {
        /// <summary>
        /// Reads a "raw" sector from DVD on HL-DT-ST drives.
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the HL-DT-ST READ DVD (RAW) response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        /// <param name="lba">Start block address.</param>
        /// <param name="transferLength">How many blocks to read.</param>
        public bool HlDtStReadRawDvd(out byte[] buffer, out byte[] senseBuffer, uint lba, uint transferLength, uint timeout, out double duration)
        {
            senseBuffer = new byte[32];
            byte[] cdb = new byte[12];
            buffer = new byte[2064 * transferLength];
            bool sense;

            cdb[0] = (byte)ScsiCommands.HlDtSt_Vendor;
            cdb[1] = 0x48;
            cdb[2] = 0x49;
            cdb[3] = 0x54;
            cdb[4] = 0x01;
            cdb[6] = (byte)((lba & 0xFF000000) >> 24);
            cdb[7] = (byte)((lba & 0xFF0000) >> 16);
            cdb[8] = (byte)((lba & 0xFF00) >> 8);
            cdb[9] = (byte)(lba & 0xFF);
            cdb[10] = (byte)((buffer.Length & 0xFF00) >> 8);
            cdb[11] = (byte)(buffer.Length & 0xFF);

            lastError = SendScsiCommand(cdb, ref buffer, out senseBuffer, timeout, ScsiDirection.In, out duration, out sense);
            error = lastError != 0;

            DicConsole.DebugWriteLine("SCSI Device", "HL-DT-ST READ DVD (RAW) took {0} ms.", duration);

            return sense;
        }
    }
}

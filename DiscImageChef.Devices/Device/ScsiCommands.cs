﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : ScsiCommands.cs
// Version        : 1.0
// Author(s)      : Natalia Portillo
//
// Component      : Direct device access
//
// Revision       : $Revision$
// Last change by : $Author$
// Date           : $Date$
//
// --[ Description ] ----------------------------------------------------------
//
// Contains SCSI commands
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
        /// Sends the SCSI INQUIRY command to the device using default device timeout.
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI INQUIRY response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        public bool ScsiInquiry(out byte[] buffer, out byte[] senseBuffer)
        {
            return ScsiInquiry(out buffer, out senseBuffer, Timeout);
        }

        /// <summary>
        /// Sends the SCSI INQUIRY command to the device using default device timeout.
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI INQUIRY response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        public bool ScsiInquiry(out byte[] buffer, out byte[] senseBuffer, out double duration)
        {
            return ScsiInquiry(out buffer, out senseBuffer, Timeout, out duration);
        }

        /// <summary>
        /// Sends the SCSI INQUIRY command to the device.
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI INQUIRY response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        public bool ScsiInquiry(out byte[] buffer, out byte[] senseBuffer, uint timeout)
        {
            double duration;
            return ScsiInquiry(out buffer, out senseBuffer, timeout, out duration);
        }

        /// <summary>
        /// Sends the SCSI INQUIRY command to the device.
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI INQUIRY response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        public bool ScsiInquiry(out byte[] buffer, out byte[] senseBuffer, uint timeout, out double duration)
        {
            buffer = new byte[5];
            senseBuffer = new byte[32];
            byte[] cdb = { (byte)ScsiCommands.Inquiry, 0, 0, 0, 5, 0 };
            bool sense;

            lastError = SendScsiCommand(cdb, ref buffer, out senseBuffer, timeout, ScsiDirection.In, out duration, out sense);
            error = lastError != 0;

            if (sense)
                return true;

            byte pagesLength = (byte)(buffer[4] + 5);

            cdb = new byte[] { (byte)ScsiCommands.Inquiry, 0, 0, 0, pagesLength, 0 };
            buffer = new byte[pagesLength];
            senseBuffer = new byte[32];

            lastError = SendScsiCommand(cdb, ref buffer, out senseBuffer, timeout, ScsiDirection.In, out duration, out sense);
            error = lastError != 0;

            DicConsole.DebugWriteLine("SCSI Device", "INQUIRY took {0} ms.", duration);

            return sense;
        }

        /// <summary>
        /// Sends the SCSI INQUIRY command to the device with an Extended Vital Product Data page using default device timeout.
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI INQUIRY response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="page">The Extended Vital Product Data</param>
        public bool ScsiInquiry(out byte[] buffer, out byte[] senseBuffer, byte page)
        {
            return ScsiInquiry(out buffer, out senseBuffer, page, Timeout);
        }

        /// <summary>
        /// Sends the SCSI INQUIRY command to the device with an Extended Vital Product Data page using default device timeout.
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI INQUIRY response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        /// <param name="page">The Extended Vital Product Data</param>
        public bool ScsiInquiry(out byte[] buffer, out byte[] senseBuffer, byte page, out double duration)
        {
            return ScsiInquiry(out buffer, out senseBuffer, page, Timeout, out duration);
        }

        /// <summary>
        /// Sends the SCSI INQUIRY command to the device with an Extended Vital Product Data page.
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI INQUIRY response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="page">The Extended Vital Product Data</param>
        public bool ScsiInquiry(out byte[] buffer, out byte[] senseBuffer, byte page, uint timeout)
        {
            double duration;
            return ScsiInquiry(out buffer, out senseBuffer, page, timeout, out duration);
        }

        /// <summary>
        /// Sends the SCSI INQUIRY command to the device with an Extended Vital Product Data page.
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI INQUIRY response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        /// <param name="page">The Extended Vital Product Data</param>
        public bool ScsiInquiry(out byte[] buffer, out byte[] senseBuffer, byte page, uint timeout, out double duration)
        {
            buffer = new byte[5];
            senseBuffer = new byte[32];
            byte[] cdb = { (byte)ScsiCommands.Inquiry, 1, page, 0, 5, 0 };
            bool sense;

            lastError = SendScsiCommand(cdb, ref buffer, out senseBuffer, timeout, ScsiDirection.In, out duration, out sense);
            error = lastError != 0;

            if (sense)
                return true;

            byte pagesLength = (byte)(buffer[3] + 4);

            cdb = new byte[] { (byte)ScsiCommands.Inquiry, 1, page, 0, pagesLength, 0 };
            buffer = new byte[pagesLength];
            senseBuffer = new byte[32];

            lastError = SendScsiCommand(cdb, ref buffer, out senseBuffer, timeout, ScsiDirection.In, out duration, out sense);
            error = lastError != 0;

            DicConsole.DebugWriteLine("SCSI Device", "INQUIRY took {0} ms.", duration);

            return sense;
        }

        /// <summary>
        /// Sends the SCSI TEST UNIT READY command to the device
        /// </summary>
        /// <returns><c>true</c>, if unit is NOT ready, <c>false</c> otherwise.</returns>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        public bool ScsiTestUnitReady(out byte[] senseBuffer, uint timeout, out double duration)
        {
            senseBuffer = new byte[32];
            byte[] cdb = { (byte)ScsiCommands.TestUnitReady, 0, 0, 0, 0, 0 };
            bool sense;
            byte[] buffer = new byte[0];

            lastError = SendScsiCommand(cdb, ref buffer, out senseBuffer, timeout, ScsiDirection.None, out duration, out sense);
            error = lastError != 0;

            DicConsole.DebugWriteLine("SCSI Device", "TEST UNIT READY took {0} ms.", duration);

            return sense;
        }

        /// <summary>
        /// Sends the SCSI MODE SENSE(6) command to the device as introduced in SCSI-1
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI MODE SENSE(6) response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        public bool ModeSense(out byte[] buffer, out byte[] senseBuffer, uint timeout, out double duration)
        {
            return ModeSense6(out buffer, out senseBuffer, false, ScsiModeSensePageControl.Current, 0, 0, timeout, out duration);
        }

        /// <summary>
        /// Sends the SCSI MODE SENSE(6) command to the device as introduced in SCSI-2
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI MODE SENSE(6) response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        /// <param name="DBD">If set to <c>true</c> device MUST not return any block descriptor.</param>
        /// <param name="pageControl">Page control.</param>
        /// <param name="pageCode">Page code.</param>
        public bool ModeSense6(out byte[] buffer, out byte[] senseBuffer, bool DBD, ScsiModeSensePageControl pageControl, byte pageCode, uint timeout, out double duration)
        {
            return ModeSense6(out buffer, out senseBuffer, DBD, pageControl, pageCode, 0, timeout, out duration);
        }

        /// <summary>
        /// Sends the SCSI MODE SENSE(6) command to the device as introduced in SCSI-3 SPC-3
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI MODE SENSE(6) response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        /// <param name="DBD">If set to <c>true</c> device MUST not return any block descriptor.</param>
        /// <param name="pageControl">Page control.</param>
        /// <param name="pageCode">Page code.</param>
        /// <param name="subPageCode">Sub-page code.</param>
        public bool ModeSense6(out byte[] buffer, out byte[] senseBuffer, bool DBD, ScsiModeSensePageControl pageControl, byte pageCode, byte subPageCode, uint timeout, out double duration)
        {
            senseBuffer = new byte[32];
            byte[] cdb = new byte[6];
            buffer = new byte[4];
            bool sense;

            cdb[0] = (byte)ScsiCommands.ModeSense;
            if (DBD)
                cdb[1] = 0x08;
            cdb[2] |= (byte)pageControl;
            cdb[2] |= (byte)(pageCode & 0x3F);
            cdb[3] = subPageCode;
            cdb[4] = (byte)buffer.Length;
            cdb[5] = 0;

            lastError = SendScsiCommand(cdb, ref buffer, out senseBuffer, timeout, ScsiDirection.In, out duration, out sense);
            error = lastError != 0;

            if (sense)
                return true;

            byte modeLength = (byte)(buffer[0] + 1);
            buffer = new byte[modeLength];
            cdb[4] = (byte)buffer.Length;
            senseBuffer = new byte[32];

            lastError = SendScsiCommand(cdb, ref buffer, out senseBuffer, timeout, ScsiDirection.In, out duration, out sense);
            error = lastError != 0;

            DicConsole.DebugWriteLine("SCSI Device", "MODE SENSE(6) took {0} ms.", duration);

            return sense;
        }

        /// <summary>
        /// Sends the SCSI MODE SENSE(10) command to the device as introduced in SCSI-2
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI MODE SENSE(10) response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        /// <param name="DBD">If set to <c>true</c> device MUST not return any block descriptor.</param>
        /// <param name="pageControl">Page control.</param>
        /// <param name="pageCode">Page code.</param>
        public bool ModeSense10(out byte[] buffer, out byte[] senseBuffer, bool DBD, ScsiModeSensePageControl pageControl, byte pageCode, uint timeout, out double duration)
        {
            return ModeSense10(out buffer, out senseBuffer, false, DBD, pageControl, pageCode, 0, timeout, out duration);
        }

        /// <summary>
        /// Sends the SCSI MODE SENSE(10) command to the device as introduced in SCSI-3 SPC-2
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI MODE SENSE(10) response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        /// <param name="DBD">If set to <c>true</c> device MUST not return any block descriptor.</param>
        /// <param name="pageControl">Page control.</param>
        /// <param name="pageCode">Page code.</param>
        /// <param name="LLBAA">If set means 64-bit LBAs are accepted by the caller.</param>
        public bool ModeSense10(out byte[] buffer, out byte[] senseBuffer, bool LLBAA, bool DBD, ScsiModeSensePageControl pageControl, byte pageCode, uint timeout, out double duration)
        {
            return ModeSense10(out buffer, out senseBuffer, LLBAA, DBD, pageControl, pageCode, 0, timeout, out duration);
        }

        /// <summary>
        /// Sends the SCSI MODE SENSE(10) command to the device as introduced in SCSI-3 SPC-3
        /// </summary>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="buffer">Buffer where the SCSI MODE SENSE(10) response will be stored</param>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        /// <param name="DBD">If set to <c>true</c> device MUST not return any block descriptor.</param>
        /// <param name="pageControl">Page control.</param>
        /// <param name="pageCode">Page code.</param>
        /// <param name="subPageCode">Sub-page code.</param>
        /// <param name="LLBAA">If set means 64-bit LBAs are accepted by the caller.</param>
        public bool ModeSense10(out byte[] buffer, out byte[] senseBuffer, bool LLBAA, bool DBD, ScsiModeSensePageControl pageControl, byte pageCode, byte subPageCode, uint timeout, out double duration)
        {
            senseBuffer = new byte[32];
            byte[] cdb = new byte[10];
            buffer = new byte[4];
            bool sense;

            cdb[0] = (byte)ScsiCommands.ModeSense10;
            if (LLBAA)
                cdb[1] |= 0x10;
            if (DBD)
                cdb[1] |= 0x08;
            cdb[2] |= (byte)pageControl;
            cdb[2] |= (byte)(pageCode & 0x3F);
            cdb[3] = subPageCode;
            cdb[7] = (byte)((buffer.Length & 0xFF00) >> 8);
            cdb[8] = (byte)(buffer.Length & 0xFF);
            cdb[9] = 0;

            lastError = SendScsiCommand(cdb, ref buffer, out senseBuffer, timeout, ScsiDirection.In, out duration, out sense);
            error = lastError != 0;

            if (sense)
                return true;

            ushort modeLength = (ushort)(((int)buffer[0] << 8) + buffer[1] + 2);
            buffer = new byte[modeLength];
            cdb[7] = (byte)((buffer.Length & 0xFF00) >> 8);
            cdb[8] = (byte)(buffer.Length & 0xFF);
            senseBuffer = new byte[32];

            lastError = SendScsiCommand(cdb, ref buffer, out senseBuffer, timeout, ScsiDirection.In, out duration, out sense);
            error = lastError != 0;

            DicConsole.DebugWriteLine("SCSI Device", "MODE SENSE(10) took {0} ms.", duration);

            return sense;
        }

        /// <summary>
        /// Sends the SCSI PREVENT ALLOW MEDIUM REMOVAL command to prevent medium removal
        /// </summary>
        /// <returns><c>true</c>, if allow medium removal was prevented, <c>false</c> otherwise.</returns>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        public bool PreventMediumRemoval(out byte[] senseBuffer, uint timeout, out double duration)
        {
            return PreventAllowMediumRemoval(out senseBuffer, ScsiPreventAllowMode.Prevent, timeout, out duration);
        }

        /// <summary>
        /// Sends the SCSI PREVENT ALLOW MEDIUM REMOVAL command to allow medium removal
        /// </summary>
        /// <returns><c>true</c>, if allow medium removal was prevented, <c>false</c> otherwise.</returns>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        public bool AllowMediumRemoval(out byte[] senseBuffer, uint timeout, out double duration)
        {
            return PreventAllowMediumRemoval(out senseBuffer, ScsiPreventAllowMode.Allow, timeout, out duration);
        }

        /// <summary>
        /// Sends the SCSI PREVENT ALLOW MEDIUM REMOVAL command
        /// </summary>
        /// <returns><c>true</c>, if allow medium removal was prevented, <c>false</c> otherwise.</returns>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        /// <param name="prevent"><c>true</c> to prevent medium removal, <c>false</c> to allow it.</param>
        public bool PreventAllowMediumRemoval(out byte[] senseBuffer, bool prevent, uint timeout, out double duration)
        {
            if (prevent)
                return PreventAllowMediumRemoval(out senseBuffer, ScsiPreventAllowMode.Prevent, timeout, out duration);
            else
                return PreventAllowMediumRemoval(out senseBuffer, ScsiPreventAllowMode.Allow, timeout, out duration);
        }

        /// <summary>
        /// Sends the SCSI PREVENT ALLOW MEDIUM REMOVAL command
        /// </summary>
        /// <returns><c>true</c>, if allow medium removal was prevented, <c>false</c> otherwise.</returns>
        /// <returns><c>true</c> if the command failed and <paramref name="senseBuffer"/> contains the sense buffer.</returns>
        /// <param name="senseBuffer">Sense buffer.</param>
        /// <param name="timeout">Timeout in seconds.</param>
        /// <param name="duration">Duration in milliseconds it took for the device to execute the command.</param>
        /// <param name="preventMode">Prevention mode.</param>
        public bool PreventAllowMediumRemoval(out byte[] senseBuffer, ScsiPreventAllowMode preventMode, uint timeout, out double duration)
        {
            senseBuffer = new byte[32];
            byte[] cdb = new byte[6];
            bool sense;
            byte[] buffer = new byte[0];

            cdb[0] = (byte)ScsiCommands.PreventAllowMediumRemoval;
            cdb[4] = (byte)((byte)preventMode & 0x03);

            lastError = SendScsiCommand(cdb, ref buffer, out senseBuffer, timeout, ScsiDirection.None, out duration, out sense);
            error = lastError != 0;

            DicConsole.DebugWriteLine("SCSI Device", "PREVENT ALLOW MEDIUM REMOVAL took {0} ms.", duration);

            return sense;
        }
    }
}

﻿// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {
  internal class WindowsSmart : ISmart {
    private readonly int driveNumber;
    private readonly SafeHandle handle;

    public WindowsSmart(int driveNumber) {
      this.driveNumber = driveNumber;
      handle = Kernel32.CreateFile(@"\\.\PhysicalDrive" + driveNumber,
                                   FileAccess.ReadWrite,
                                   FileShare.ReadWrite,
                                   IntPtr.Zero,
                                   FileMode.Open,
                                   FileAttributes.Normal,
                                   IntPtr.Zero);
    }

    public bool IsValid => !handle.IsInvalid;

    public void Dispose() {
      Close();
    }

    public void Close() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    public bool EnableSmart() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");


      var parameter = new Kernel32.DriveCommandParameter {
        DriveNumber = (byte) driveNumber,
        Registers = {
          Features = Kernel32.RegisterFeature.SmartEnableOperations,
          LBAMid = Kernel32.SMART_LBA_MID,
          LBAHigh = Kernel32.SMART_LBA_HI,
          Command = Kernel32.RegisterCommand.SmartCmd
        }
      };


      return Kernel32.DeviceIoControl(
                                      handle,
                                      Kernel32.DriveCommand.SendDriveCommand,
                                      ref parameter,
                                      Marshal.SizeOf(parameter),
                                      out Kernel32.DriveCommandResult _,
                                      Marshal.SizeOf<Kernel32.DriveCommandResult>(),
                                      out _,
                                      IntPtr.Zero);
    }

    public Kernel32.DriveAttributeValue[] ReadSmartData() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");


      var parameter = new Kernel32.DriveCommandParameter {
        DriveNumber = (byte) driveNumber,
        Registers = {
          Features = Kernel32.RegisterFeature.SmartReadData,
          LBAMid = Kernel32.SMART_LBA_MID,
          LBAHigh = Kernel32.SMART_LBA_HI,
          Command = Kernel32.RegisterCommand.SmartCmd
        }
      };


      bool isValid = Kernel32.DeviceIoControl(handle,
                                              Kernel32.DriveCommand.ReceiveDriveData,
                                              ref parameter,
                                              Marshal.SizeOf(parameter),
                                              out Kernel32.DriveSmartReadDataResult result,
                                              Marshal.SizeOf<Kernel32.DriveSmartReadDataResult>(),
                                              out _,
                                              IntPtr.Zero);

      return (isValid) ? result.Attributes : new Kernel32.DriveAttributeValue[0];
    }

    public Kernel32.DriveThresholdValue[] ReadSmartThresholds() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");


      var parameter = new Kernel32.DriveCommandParameter {
        DriveNumber = (byte) driveNumber,
        Registers = {
          Features = Kernel32.RegisterFeature.SmartReadThresholds,
          LBAMid = Kernel32.SMART_LBA_MID,
          LBAHigh = Kernel32.SMART_LBA_HI,
          Command = Kernel32.RegisterCommand.SmartCmd
        }
      };


      bool isValid = Kernel32.DeviceIoControl(handle,
                                              Kernel32.DriveCommand.ReceiveDriveData,
                                              ref parameter,
                                              Marshal.SizeOf(parameter),
                                              out Kernel32.DriveSmartReadThresholdsResult result,
                                              Marshal.SizeOf<Kernel32.DriveSmartReadThresholdsResult>(),
                                              out _,
                                              IntPtr.Zero);

      return (isValid) ? result.Thresholds : new Kernel32.DriveThresholdValue[0];
    }

    public bool ReadNameAndFirmwareRevision(out string name, out string firmwareRevision) {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");


      var parameter = new Kernel32.DriveCommandParameter { DriveNumber = (byte) driveNumber, Registers = { Command = Kernel32.RegisterCommand.IdCmd } };


      bool valid = Kernel32.DeviceIoControl(handle,
                                            Kernel32.DriveCommand.ReceiveDriveData,
                                            ref parameter,
                                            Marshal.SizeOf(parameter),
                                            out Kernel32.DriveIdentifyResult result,
                                            Marshal.SizeOf<Kernel32.DriveIdentifyResult>(),
                                            out _,
                                            IntPtr.Zero);

      if (!valid) {
        name = null;
        firmwareRevision = null;
        return false;
      }

      name = GetString(result.Identify.ModelNumber);
      firmwareRevision = GetString(result.Identify.FirmwareRevision);
      return true;
    }

    protected void Dispose(bool disposing) {
      if (disposing) {
        if (!handle.IsClosed)
          handle.Close();
      }
    }

    private string GetString(byte[] bytes) {
      char[] chars = new char[bytes.Length];
      for (int i = 0; i < bytes.Length; i += 2) {
        chars[i] = (char) bytes[i + 1];
        chars[i + 1] = (char) bytes[i];
      }

      return new string(chars).Trim(' ', '\0');
    }
  }
}
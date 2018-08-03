﻿/*
   MIT License
   
   Copyright (c) 2018 Berkay Yigit
   
   Permission is hereby granted, free of charge, to any person obtaining a copy
   of this software and associated documentation files (the "Software"), to deal
   in the Software without restriction, including without limitation the rights
   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
   copies of the Software, and to permit persons to whom the Software is
   furnished to do so, subject to the following conditions:
   
   The above copyright notice and this permission notice shall be included in all
   copies or substantial portions of the Software.
   
   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace PlayEngine.Helpers {
   public class Memory {
      public class Sections {
         public static List<librpc.MemorySection> getMemorySections(librpc.ProcessInfo processInfo, librpc.VM_PROT protection = librpc.VM_PROT.READ) {
            var listMemoryEntries = new List<librpc.MemorySection>();
            foreach (var memorySection in processInfo.listProcessMemorySections)
               if ((memorySection.protection & protection) == protection)
                  listMemoryEntries.Add(memorySection);

            return listMemoryEntries;
         }
         public static librpc.MemorySection findMemorySectionByName(librpc.ProcessInfo processInfo, String sectionName, librpc.VM_PROT protection = librpc.VM_PROT.READ) {
            librpc.MemorySection result = null;
            foreach (var memorySection in processInfo.listProcessMemorySections) {
               if (memorySection.name == sectionName &&
                   (memorySection.protection & protection) == protection) {
                  result = memorySection;
                  break;
               }
            }

            return result;
         }
      }
      public class ActiveProcess {
         public static String getId() {
            librpc.ProcessInfo processInfo = Memory.ps4RPC.GetProcessInfo("SceCdlgApp");
            librpc.MemorySection memorySection = Sections.findMemorySectionByName(processInfo, "libSceCdlgUtilServer.sprx", librpc.VM_PROT.RW);
            if (memorySection == null)
               return String.Empty;

            return Memory.readString(processInfo.id, memorySection.start + 0xA0);
         }
         public static String getVersionStr() {
            librpc.ProcessInfo processInfo = Memory.ps4RPC.GetProcessInfo("SceCdlgApp");
            librpc.MemorySection memorySection = Sections.findMemorySectionByName(processInfo, "libSceCdlgUtilServer.sprx", librpc.VM_PROT.RW);
            if (memorySection == null)
               return String.Empty;

            return Memory.readString(processInfo.id, memorySection.start + 0xC8);
         }
      }

      public enum CompareType {
         None,
         ExactValue,
         FuzzyValue,
         IncreasedValue,
         IncreasedValueBy,
         DecreasedValue,
         DecreasedValueBy,
         BiggerThan,
         SmallerThan,
         ChangedValue,
         UnchangedValue,
         BetweenValues,
         UnknownInitialValue
      }
      public static class CompareUtil {
         public static Boolean compare(dynamic searchValue, dynamic memoryValueToCompare, dynamic previousMemoryValue, CompareType compareType, dynamic[] extraParams = null) {
            Boolean isMatched = false;
            switch (compareType) {
               case CompareType.ExactValue:
                  isMatched = searchValue == memoryValueToCompare;
                  break;
               case CompareType.FuzzyValue:
                  isMatched = Math.Abs(searchValue - memoryValueToCompare) < 1.0f;
                  break;
               case CompareType.IncreasedValue:
                  isMatched = memoryValueToCompare > previousMemoryValue;
                  break;
               case CompareType.IncreasedValueBy:
                  isMatched = memoryValueToCompare == previousMemoryValue + searchValue;
                  break;
               case CompareType.DecreasedValue:
                  isMatched = memoryValueToCompare < previousMemoryValue;
                  break;
               case CompareType.DecreasedValueBy:
                  isMatched = memoryValueToCompare == previousMemoryValue - searchValue;
                  break;
               case CompareType.BiggerThan:
                  isMatched = searchValue > memoryValueToCompare;
                  break;
               case CompareType.SmallerThan:
                  isMatched = searchValue < memoryValueToCompare;
                  break;
               case CompareType.ChangedValue:
                  isMatched = memoryValueToCompare != previousMemoryValue;
                  break;
               case CompareType.UnchangedValue:
                  isMatched = memoryValueToCompare == previousMemoryValue;
                  break;
               case CompareType.BetweenValues:
                  isMatched = (memoryValueToCompare > extraParams[0]) && (memoryValueToCompare < extraParams[1]);
                  break;
               case CompareType.None:
               case CompareType.UnknownInitialValue:
                  isMatched = true;
                  break;
            }
            return isMatched;
         }
      }

      private static Mutex mutex = new Mutex();
      public static librpc.PS4RPC ps4RPC = null;
      public static Boolean initPS4RPC(String ipAddress) {
         try {
            mutex.WaitOne();
            if (ps4RPC != null)
               ps4RPC.Disconnect();
            ps4RPC = new librpc.PS4RPC(ipAddress);
            ps4RPC.Connect();
         } catch {
         } finally {
            mutex.ReleaseMutex();
         }
         return ps4RPC != null;
      }
      public static librpc.ProcessInfo getProcessInfoFromName(String processName) {
         return Memory.ps4RPC.GetProcessInfo(Memory.ps4RPC.GetProcessList().Where(proc => proc.name == processName).First().id);
      }

      public static Byte[] readByteArray(Int32 procId, UInt64 address, Int32 size) {
         Byte[] returnBuf = null;
         try {
            mutex.WaitOne();
            returnBuf = ps4RPC.ReadMemory(procId, address, size);
         } catch (Exception ex) {
            Console.WriteLine("Error during ReadByteArray:\r\nProcessId: {0}, Address: {1}, Size: {2}\r\n{3}",
                procId, address.ToString("X"), size, ex.ToString());
         } finally {
            mutex.ReleaseMutex();
         }
         return returnBuf;
      }
      public static String readString(Int32 procId, UInt64 address) {
         String returnStr = String.Empty;
         try {
            mutex.WaitOne();
            returnStr = ps4RPC.ReadString(procId, address);
         } catch (Exception ex) {
            Console.WriteLine("Error during ReadString:\r\nProcessId: {0}, Address: {1}\r\n{2}",
                procId, address.ToString("X"), ex.ToString());
         } finally {
            mutex.ReleaseMutex();
         }
         return returnStr;
      }
      public static dynamic read(Int32 procId, UInt64 address, Type valueType) {
         if (valueType == typeof(String))
            return readString(procId, address);
         Byte[] readBuffer = readByteArray(procId, address, valueType == typeof(Boolean) ? 1 : Marshal.SizeOf(valueType));
         return readBuffer == null ? -1 : readBuffer.getObject(valueType);
      }

      public static void writeByteArray(Int32 procId, UInt64 address, Byte[] bytes) {
         try {
            mutex.WaitOne();
            ps4RPC.WriteMemory(procId, address, bytes);
         } catch (Exception ex) {
            Console.WriteLine("Error during WriteByteArray:\r\nProcessId: {0}, Address: {1}, bytes.Length: {2}\r\n{3}",
                   procId, address.ToString("X"), bytes.Length, ex.ToString());
         } finally {
            mutex.ReleaseMutex();
         }
      }
      public static void writeString(Int32 procId, UInt64 address, String str) {
         try {
            mutex.WaitOne();
            ps4RPC.WriteString(procId, address, str);
         } catch (Exception ex) {
            Console.WriteLine("Error during WriteString:\r\nProcessId: {0}, Address: {1}, String: {2}\r\n{3}",
                procId, address.ToString("X"), str, ex.ToString());
         } finally {
            mutex.ReleaseMutex();
         }
      }
      public static void write(Int32 procId, UInt64 address, Object value, Type valueType) {
         if (valueType == typeof(String))
            writeString(procId, address, (String)value);
         else
            writeByteArray(procId, address, value.getBytes(valueType));
      }

      public static List<Tuple<UInt32, dynamic>> scan(Byte[] scanSearchBuffer, dynamic scanValue, Type scanValueType, CompareType scanCompareType, dynamic[] extraParams, Int32 maxResultsCount) {
         List<Tuple<UInt32, dynamic>> listResults = new List<Tuple<UInt32, dynamic>>();
         Int32 objectTypeSize = 0;
         Func<Int32 /* scanSearchBufferIndex */, dynamic /* returnValue */> fnGetMemoryValue = null;

         switch (Type.GetTypeCode(scanValueType)) {
            case TypeCode.SByte: {
               fnGetMemoryValue = (iBuffer) => scanSearchBuffer[iBuffer];
               objectTypeSize = sizeof(SByte);
            }
            break;
            case TypeCode.Byte: {
               fnGetMemoryValue = (iBuffer) => scanSearchBuffer[iBuffer];
               objectTypeSize = sizeof(Byte);
            }
            break;
            case TypeCode.Boolean: {
               fnGetMemoryValue = (iBuffer) => BitConverter.ToBoolean(scanSearchBuffer, iBuffer);
               objectTypeSize = 1;
            }
            break;
            case TypeCode.Int16: {
               fnGetMemoryValue = (iBuffer) => BitConverter.ToInt16(scanSearchBuffer, iBuffer);
               objectTypeSize = sizeof(Int16);
            }
            break;
            case TypeCode.UInt16: {
               fnGetMemoryValue = (iBuffer) => BitConverter.ToUInt16(scanSearchBuffer, iBuffer);
               objectTypeSize = sizeof(UInt16);
            }
            break;
            case TypeCode.Int32: {
               fnGetMemoryValue = (iBuffer) => BitConverter.ToUInt32(scanSearchBuffer, iBuffer);
               objectTypeSize = sizeof(Int32);
            }
            break;
            case TypeCode.UInt32: {
               fnGetMemoryValue = (iBuffer) => BitConverter.ToUInt32(scanSearchBuffer, iBuffer);
               objectTypeSize = sizeof(UInt32);
            }
            break;
            case TypeCode.Int64: {
               fnGetMemoryValue = (iBuffer) => BitConverter.ToUInt64(scanSearchBuffer, iBuffer);
               objectTypeSize = sizeof(Int64);
            }
            break;
            case TypeCode.UInt64: {
               fnGetMemoryValue = (iBuffer) => BitConverter.ToUInt64(scanSearchBuffer, iBuffer);
               objectTypeSize = sizeof(UInt64);
            }
            break;
            case TypeCode.Double: {
               fnGetMemoryValue = (iBuffer) => BitConverter.ToDouble(scanSearchBuffer, iBuffer);
               objectTypeSize = sizeof(Double);
            }
            break;
            case TypeCode.Single: {
               fnGetMemoryValue = (iBuffer) => BitConverter.ToSingle(scanSearchBuffer, iBuffer);
               objectTypeSize = sizeof(Single);
            }
            break;
            case TypeCode.String: {
               var scanResults = scan(scanSearchBuffer, ((Object)scanValue).getBytes(typeof(String)), typeof(Byte[]), scanCompareType, extraParams, maxResultsCount);
               foreach (Tuple<UInt32, dynamic> tuple in scanResults)
                  listResults.Add(new Tuple<UInt32, dynamic>(tuple.Item1, Encoding.ASCII.GetString(tuple.Item2)));
               return listResults;
            }
         }
         if (scanValueType == typeof(Byte[])) {
            Byte[] scanValueBuffer = scanValue;
            Int32 indexEnd = scanSearchBuffer.Length - scanValueBuffer.Length;
            for (Int32 index = 0; index < indexEnd; index += scanValueBuffer.Length) {
               Boolean isFound = false;
               for (Int32 j = 0; j < scanValueBuffer.Length - 1; j++) {
                  isFound = scanSearchBuffer[index + j] == scanValueBuffer[j];
                  if (!isFound)
                     break;
               }
               if (isFound)
                  listResults.Add(new Tuple<UInt32, dynamic>((UInt32)index, scanValueBuffer));
               if (listResults.Count > maxResultsCount)
                  break;
            }
         } else {
            Int32 endOffset = scanSearchBuffer.Length - objectTypeSize;
            for (Int32 index = 0; index < endOffset; index += objectTypeSize) {
               dynamic memoryValue = fnGetMemoryValue.Invoke(index);
               if (Memory.CompareUtil.compare(scanValue, memoryValue, null, scanCompareType, extraParams))
                  listResults.Add(new Tuple<UInt32, dynamic>((UInt32)index, memoryValue));
               if (listResults.Count > maxResultsCount)
                  break;
            }
         }
         return listResults;
      }
   }
}

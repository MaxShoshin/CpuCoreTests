using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace CpuThreadingTest.ConsoleApp
{
    	// http://multiproc.codeplex.com/

	// Ref: http://blogs.msdn.com/b/virtual_pc_guy/archive/2010/08/13/using-powershell-to-find-the-virtual-processor-to-logical-processor-ratio-of-hyper-v.aspx
	// Ref excerpt:
	// Power Shell command:
	// write-host (@(gwmi -ns root\virtualization MSVM_Processor).count / (@(gwmi Win32_Processor) | measure -p NumberOfLogicalProcessors -sum).Sum) "virtual processor(s) per logical processor" -f yellow

	/*
		But what does this line of code do – and how does it work?  Well – there are a couple of things to know:

		For each virtual processor for each currently running virtual machine there is an instance of the MSVM_Processor WMI object on the system.  So to figure out how many virtual processors are currently running – you just have to count how many instances of this WMI object are currently present on the system.
		Summing up the NumberOfLogicalProcessors property of the Win32_Processor object will given you the other side of the calculation – namely how many logical processors are present in the system.
		In order to keep the one line of code as short as possible – I have used every PowerShell abbreviation that I know of (gwmi for Get-WMIObject, –ns for –namespace, measure for measure-object, –p for –property, –f for –foregroundColor)
	 */

	public class SystemInfoHelper
	{
		public static int WmiPhysicalProcessorCount
		{
			get
			{
				var searcherCpuCount = new ManagementObjectSearcher("Select * from Win32_ComputerSystem");
				int processorCount = -1;
				try
				{
					foreach (var res in searcherCpuCount.Get())
					{
						processorCount = Convert.ToInt32(res["NumberOfProcessors"]);
					}
				}
				catch (Exception)
				{
				}

				return processorCount;
			}
		}

		/// <summary>
		/// Per processor
		/// </summary>
		/// <value></value>
		public static int WmiCoreCount
		{
			get
			{
				var searcherCoreCount = new ManagementObjectSearcher("Select * from Win32_Processor");
				int coreCount = -1;
				try
				{
					foreach (var res in searcherCoreCount.Get())
					{
						coreCount = Convert.ToInt32(res["NumberOfCores"]);
					}
				}
				catch (Exception)
				{
				}

				return coreCount;
			}
		}

		/// <summary>
		/// Global
		/// </summary>
		/// <value></value>
		public static int WmiGlobalLogicalProcessorCount
		{
			get
			{
				var searcherCpuCount = new ManagementObjectSearcher("Select * from Win32_ComputerSystem");
				int processorCount = -1;

				try
				{
					foreach (var res in searcherCpuCount.Get())
					{
						processorCount = Convert.ToInt32(res["NumberOfLogicalProcessors"]);
					}
				}
				catch (Exception)
				{
				}

				return processorCount;
			}
		}

		public static int LogicalProcessorCountFromCsharpEnvironment
		{
			get { return Environment.ProcessorCount; }
		}

		/// <summary>
		/// Althought not garanty to be exact as per documentation, that's the simplest way to
		/// know the number of ProcessorGroup (Eric Ouellet)
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GetNumaHighestNodeNumber(out uint count);

		public static int NumaHighestNodeNumber
		{
			get
			{
				uint numaHighestNodeNumber = 0;
				try
				{
					GetNumaHighestNodeNumber(out numaHighestNodeNumber);
				}
				catch (Exception)
				{
					return -1;
				}

				return (int) numaHighestNodeNumber; // Node number start ar 0
			}
		}

		[DllImport("kernel32.dll")]
		public static extern UInt16 GetActiveProcessorGroupCount();

		[DllImport("kernel32.dll")]
		public static extern UInt16 GetMaximumProcessorGroupCount();


		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetProcessAffinityMask(IntPtr hProcess,
			out UInt64 lpProcessAffinityMask, out UInt64 lpSystemAffinityMask);


		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern UInt32 GetActiveProcessorCount(UInt16 groupNumber);


		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetNumaNodeProcessorMask(byte node, out UInt64 processorMask);

		/// <summary>
		/// By thread
		/// </summary>
		/// <returns></returns>
		[DllImport("kernel32.dll")]
		public static extern UInt32 GetCurrentProcessorNumber();

		public struct PROCESSOR_NUMBER
		{
			public UInt16 Group;
			public byte Number;
			public byte Reserved;
		}

		[DllImport("kernel32.dll")]
		public static extern void GetCurrentProcessorNumberEx(ref PROCESSOR_NUMBER processorNumber);


		/// <summary>
		/// Start: From David Hefffernan on StackOverflow: Code to get GetLogicalProcessorInformation
		///
		/// EO: I Think it is buggy: I receive 27 processor groups on a machine with 16
		/// </summary>

		public enum PROCESSOR_CACHE_TYPE // 4
		{
			Unified = 0,
			Instruction = 1,
			Data = 2,
			Trace = 3,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CACHE_DESCRIPTOR // 16
		{
			public byte Level; // 1
			public byte Associativity; // 1
			public UInt16 LineSize; // 2
			public UInt32 Size; // 4
			public PROCESSOR_CACHE_TYPE Type; // 8
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PROCESSOR_CORE // 1
		{
			public byte Flags;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct NUMA_NODE // 4
		{
			public UInt32 NodeNumber;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION // 16
		{
			[FieldOffset(0)] public PROCESSOR_CORE ProcessorCore; // 1
			[FieldOffset(0)] public NUMA_NODE NumaNode; // 4
			[FieldOffset(0)] public CACHE_DESCRIPTOR Cache; // 16
			[FieldOffset(0)] private UInt64 Reserved1; // 8
			[FieldOffset(8)] private UInt64 Reserved2; // 8
		}

		public enum LOGICAL_PROCESSOR_RELATIONSHIP // 4
		{
			RelationProcessorCore,
			RelationNumaNode,
			RelationCache,
			RelationProcessorPackage,
			RelationGroup,
			RelationAll = 0xffff
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION // 32
		{
			public UIntPtr ProcessorMask; // 8
			public LOGICAL_PROCESSOR_RELATIONSHIP Relationship; // 8
			public SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION ProcessorInformation; // 16
		}

		[StructLayout(LayoutKind.Sequential)] // 16
		public struct GROUP_AFFINITY // 16
		{
			public UIntPtr Mask; // 8 (in x64)

			public UInt16 Group; // 2
			// [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			// public UInt16[] Reserved; // 6

			public UInt16 Reserved1;
			public UInt16 Reserved2;
			public UInt16 Reserved3;
		}

		[StructLayout(LayoutKind.Explicit)] // 40
		public struct PROCESSOR_RELATIONSHIP_INTERNAL
		{
			[FieldOffset(0)] public byte Flags; // 1

			//[MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
			//public byte[] Reserved; // 21

			[FieldOffset(22)] public UInt16 GroupCount; // 2

			//[FieldOffset(24)]
			//[MarshalAs(UnmanagedType.ByValArray)] // To verify: [ANYSIZE_ARRAY]
			//public GROUP_AFFINITY[] GroupMask;
		}

		public struct PROCESSOR_RELATIONSHIP
		{
			public Byte Flags;
			public UInt16 GroupCount;
			public GROUP_AFFINITY[] GroupAffinity;
		}

		[StructLayout(LayoutKind.Sequential)] // 40
		public struct NUMA_NODE_RELATIONSHIP
		{
			public UInt32 NodeNumber; // 4
			//[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
			//public byte[] Reserved; // 20

			public byte Reserved01;
			public byte Reserved02;
			public byte Reserved03;
			public byte Reserved04;
			public byte Reserved05;
			public byte Reserved06;
			public byte Reserved07;
			public byte Reserved08;
			public byte Reserved09;
			public byte Reserved10;
			public byte Reserved11;
			public byte Reserved12;
			public byte Reserved13;
			public byte Reserved14;
			public byte Reserved15;
			public byte Reserved16;
			public byte Reserved17;
			public byte Reserved18;
			public byte Reserved19;
			public byte Reserved20;

			public GROUP_AFFINITY GroupMask; // 16
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CACHE_RELATIONSHIP // 52
		{
			public byte Level; // 1
			public byte Associativity; // 1
			public UInt16 LineSize; // 2
			public UInt32 CacheSize; // 4
			public PROCESSOR_CACHE_TYPE Type; // 8

			//[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
			//public byte[] Reserved; // 20

			public byte Reserved01;
			public byte Reserved02;
			public byte Reserved03;
			public byte Reserved04;
			public byte Reserved05;
			public byte Reserved06;
			public byte Reserved07;
			public byte Reserved08;
			public byte Reserved09;
			public byte Reserved10;
			public byte Reserved11;
			public byte Reserved12;
			public byte Reserved13;
			public byte Reserved14;
			public byte Reserved15;
			public byte Reserved16;
			public byte Reserved17;
			public byte Reserved18;
			public byte Reserved19;
			public byte Reserved20;

			public GROUP_AFFINITY GroupMask; // 16

			public override string ToString()
			{
				return string.Format("Associativity: {0}, GroupMask: {1}, Level: {2}, LineSize: {3}, Type: {4}", Associativity, GroupMask, Level, LineSize, Type);
			}
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct PROCESSOR_GROUP_INFO // 48
		{
			[FieldOffset(0)] public byte MaximumProcessorCount; // 1
			[FieldOffset(1)] public byte ActiveProcessorCount; // 1

			// [MarshalAs(UnmanagedType.ByValArray, SizeConst = 38)]
			// public byte[] Reserved; // 38

			[FieldOffset(40)] public UIntPtr ActiveProcessorMask; // 8 (in x64)
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct GROUP_RELATIONSHIP_INTERNAL //
		{
			public UInt16 MaximumGroupCount; // 2
			public UInt16 ActiveGroupCount; // 2

			//[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
			//public byte[] Reserved; // 20

			public byte Reserved01;
			public byte Reserved02;
			public byte Reserved03;
			public byte Reserved04;
			public byte Reserved05;
			public byte Reserved06;
			public byte Reserved07;
			public byte Reserved08;
			public byte Reserved09;
			public byte Reserved10;
			public byte Reserved11;
			public byte Reserved12;
			public byte Reserved13;
			public byte Reserved14;
			public byte Reserved15;
			public byte Reserved16;
			public byte Reserved17;
			public byte Reserved18;
			public byte Reserved19;
			public byte Reserved20;

			// [MarshalAs(UnmanagedType.ByValArray)] // To verify: [ANYSIZE_ARRAY]
			// private PROCESSOR_GROUP_INFO[] GroupInfo; //
		}

		public struct GROUP_RELATIONSHIP
		{
			public UInt16 MaximumGroupCount;
			public UInt16 ActiveGroupCount;
			public PROCESSOR_GROUP_INFO[] GroupInfo;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_UNION
		{
			//[FieldOffset(0)]
			//public PROCESSOR_RELATIONSHIP Processor;

			//[FieldOffset(0)]
			//public NUMA_NODE_RELATIONSHIP NumaNode;

			[FieldOffset(0)] public CACHE_RELATIONSHIP Cache;

			//[FieldOffset(0)]
			//public GROUP_RELATIONSHIP Group;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
		{
			public LOGICAL_PROCESSOR_RELATIONSHIP Relationship; // 4
			public UInt32 Size; // 4
			public SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_UNION systemLogicalProcessorInformationExUnion;
		}

		public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_ROOT_INFO
		{
			public LOGICAL_PROCESSOR_RELATIONSHIP Relationship; // 4
			public UInt32 Size; // 4
		}

		[DllImport(@"kernel32.dll", SetLastError = true)]
		private static extern bool GetLogicalProcessorInformation(
			IntPtr Buffer,
			ref uint ReturnLength
		);

		private const int ERROR_INSUFFICIENT_BUFFER = 122;

		public static SYSTEM_LOGICAL_PROCESSOR_INFORMATION[] GetLogicalProcessorInformation()
		{
			uint ReturnLength = 0;
			GetLogicalProcessorInformation(IntPtr.Zero, ref ReturnLength);
			if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
			{
				IntPtr Ptr = Marshal.AllocHGlobal((int) ReturnLength);
				try
				{
					if (GetLogicalProcessorInformation(Ptr, ref ReturnLength))
					{
						int size = Marshal.SizeOf(typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
						int len = (int) ReturnLength / size;
						var buffer = new SYSTEM_LOGICAL_PROCESSOR_INFORMATION[len];
						IntPtr Item = Ptr;
						for (int i = 0; i < len; i++)
						{
							buffer[i] = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION) Marshal.PtrToStructure(Item, typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
							Item += size;
						}

						return buffer;
					}
				}
				finally
				{
					Marshal.FreeHGlobal(Ptr);
				}
			}

			return null;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GetLogicalProcessorInformationEx(
			LOGICAL_PROCESSOR_RELATIONSHIP relationshipType,
			IntPtr buffer,
			ref UInt32 returnedLength);

		// EO 2015-02-16, Function has been added by me. ~Copied from GetLogicalProcessorInformation() from David Heffernan
		public static List<T> GetLogicalProcessorInformationEx<T>() where T : struct
		{
			LOGICAL_PROCESSOR_RELATIONSHIP relationShip;

			if (typeof(T) == typeof(CACHE_RELATIONSHIP))
			{
				relationShip = LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache;
			}
			else if (typeof(T) == typeof(GROUP_RELATIONSHIP))
			{
				relationShip = LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup;
			}
			else if (typeof(T) == typeof(PROCESSOR_RELATIONSHIP))
			{
				relationShip = LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore;
			}
			else if (typeof(T) == typeof(NUMA_NODE_RELATIONSHIP))
			{
				relationShip = LOGICAL_PROCESSOR_RELATIONSHIP.RelationNumaNode;
			}
			else
			{
				throw new NotSupportedException("This type of relation is currently unsuported");
			}


			var listRelationShip = new List<T>();

			SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX systemLogicalProcessorInformationEx;
			UInt32 ReturnLength = 0;
			bool isOk = GetLogicalProcessorInformationEx(relationShip, IntPtr.Zero, ref ReturnLength);
			int error = Marshal.GetLastWin32Error();
			if (error == ERROR_INSUFFICIENT_BUFFER)
			{
				IntPtr Ptr = Marshal.AllocHGlobal((int) ReturnLength);
				try
				{
					int size = 0;

					if (GetLogicalProcessorInformationEx(relationShip, Ptr, ref ReturnLength))
					{
						var sizeLeftToRead = (Int32) ReturnLength;

						IntPtr nextItem = IntPtr.Zero;
						while (sizeLeftToRead > 0)
						{
							var structInfo =
								(SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_ROOT_INFO)
								Marshal.PtrToStructure(Ptr, typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_ROOT_INFO));

							nextItem = Ptr + Marshal.SizeOf(typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX_ROOT_INFO));

							switch (structInfo.Relationship)
							{
								case LOGICAL_PROCESSOR_RELATIONSHIP.RelationAll:
								{
									throw new NotSupportedException("LOGICAL_PROCESSOR_RELATIONSHIP.RelationAll is not supported");
								}
								case LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache: // Checked, ok
								{
									var relationship = (T) Marshal.PtrToStructure(nextItem, typeof(T));
									listRelationShip.Add(relationship);
									break;
								}
								case LOGICAL_PROCESSOR_RELATIONSHIP.RelationNumaNode: // Cheked, unsure
								{
									var relationship = (T) Marshal.PtrToStructure(nextItem, typeof(T));
									listRelationShip.Add(relationship);
									break;
								}
								case LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup:
								{
									var relationship = (GROUP_RELATIONSHIP_INTERNAL) Marshal.PtrToStructure(nextItem, typeof(GROUP_RELATIONSHIP_INTERNAL));

									IntPtr processorGroupInfoPtr = nextItem + Marshal.SizeOf(typeof(GROUP_RELATIONSHIP_INTERNAL));

									int groupCount = ((GROUP_RELATIONSHIP_INTERNAL) (object) relationship).ActiveGroupCount;
									int sizeProcessorGroupInfo = Marshal.SizeOf(typeof(PROCESSOR_GROUP_INFO));

									var processorGroupInfos = new PROCESSOR_GROUP_INFO[groupCount];

									for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
									{
										var processorGroupInfo = Marshal.PtrToStructure<PROCESSOR_GROUP_INFO>(processorGroupInfoPtr);
										processorGroupInfos[groupIndex] = processorGroupInfo;

										processorGroupInfoPtr += sizeProcessorGroupInfo;
									}

									var groupRelationship = new GROUP_RELATIONSHIP();
									groupRelationship.ActiveGroupCount = relationship.ActiveGroupCount;
									groupRelationship.MaximumGroupCount = relationship.MaximumGroupCount;
									groupRelationship.GroupInfo = processorGroupInfos;

									listRelationShip.Add((T) (object) groupRelationship);

									break;
								}
								case LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore:
								{

									var relationship = (PROCESSOR_RELATIONSHIP_INTERNAL) Marshal.PtrToStructure(nextItem, typeof(PROCESSOR_RELATIONSHIP_INTERNAL));

									IntPtr groupAffinityPtr = nextItem + Marshal.SizeOf(typeof(PROCESSOR_RELATIONSHIP_INTERNAL));

									int groupCount = ((PROCESSOR_RELATIONSHIP_INTERNAL) (object) relationship).GroupCount;
									int sizeGroupAffinity = Marshal.SizeOf(typeof(GROUP_AFFINITY));

									var processorGroupAffinitys = new GROUP_AFFINITY[groupCount];

									for (int groupMaskIndex = 0; groupMaskIndex < groupCount; groupMaskIndex++)
									{
										var groupAffinity = Marshal.PtrToStructure<GROUP_AFFINITY>(groupAffinityPtr);
										processorGroupAffinitys[groupMaskIndex] = groupAffinity;

										groupAffinityPtr += sizeGroupAffinity;
									}


									var processorRelationship = new PROCESSOR_RELATIONSHIP();
									processorRelationship.Flags = relationship.Flags;
									processorRelationship.GroupCount = relationship.GroupCount;
									processorRelationship.GroupAffinity = processorGroupAffinitys;

									listRelationShip.Add((T) (object) processorRelationship);

									break;
								}
							}

							nextItem += (int) structInfo.Size;
							sizeLeftToRead -= (int) structInfo.Size;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.Print(ex.ToString());
				}
				finally
				{
					Marshal.FreeHGlobal(Ptr);
				}
			}

			Debug.Print("Cache");
			foreach (T rel in listRelationShip)
			{
				Debug.Print(rel.ToString());
			}

			return listRelationShip;
		}

		// End: From David Hefffernan on StackOverflow: Code to get GetLogicalProcessorInformation
	}
}
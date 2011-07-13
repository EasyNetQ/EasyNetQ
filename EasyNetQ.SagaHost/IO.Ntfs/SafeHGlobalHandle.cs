using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace EasyNetQ.SagaHost.IO.Ntfs
{
	/// <summary>
	/// A <see cref="SafeHandle"/> for a global memory allocation.
	/// </summary>
	internal sealed class SafeHGlobalHandle : SafeHandle
	{
		#region Private Data

		private readonly int _size;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="SafeHGlobalHandle"/> class.
		/// </summary>
		/// <param name="toManage">
		/// The initial handle value.
		/// </param>
		/// <param name="size">
		/// The size of this memory block, in bytes.
		/// </param>
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private SafeHGlobalHandle(IntPtr toManage, int size) : base(IntPtr.Zero, true)
		{
			_size = size;
			base.SetHandle(toManage);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SafeHGlobalHandle"/> class.
		/// </summary>
		private SafeHGlobalHandle() : base(IntPtr.Zero, true)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets a value indicating whether the handle value is invalid.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the handle value is invalid;
		/// otherwise, <see langword="false"/>.
		/// </value>
		public override bool IsInvalid
		{
			get { return IntPtr.Zero == base.handle; }
		}

		/// <summary>
		/// Returns the size of this memory block.
		/// </summary>
		/// <value>
		/// The size of this memory block, in bytes.
		/// </value>
		public int Size
		{
			get { return _size; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Allocates memory from the unmanaged memory of the process using GlobalAlloc.
		/// </summary>
		/// <param name="bytes">
		/// The number of bytes in memory required.
		/// </param>
		/// <returns>
		/// A <see cref="SafeHGlobalHandle"/> representing the memory.
		/// </returns>
		/// <exception cref="OutOfMemoryException">
		/// There is insufficient memory to satisfy the request.
		/// </exception>
		public static SafeHGlobalHandle Allocate(int bytes)
		{
			return new SafeHGlobalHandle(Marshal.AllocHGlobal(bytes), bytes);
		}

		/// <summary>
		/// Returns an invalid handle.
		/// </summary>
		/// <returns>
		/// An invalid <see cref="SafeHGlobalHandle"/>.
		/// </returns>
		public static SafeHGlobalHandle Invalid()
		{
			return new SafeHGlobalHandle();
		}

		/// <summary>
		/// Executes the code required to free the handle.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the handle is released successfully;
		/// otherwise, in the event of a catastrophic failure, <see langword="false"/>.
		/// In this case, it generates a releaseHandleFailed MDA Managed Debugging Assistant.
		/// </returns>
		protected override bool ReleaseHandle()
		{
			Marshal.FreeHGlobal(base.handle);
			return true;
		}

		#endregion
	}
}

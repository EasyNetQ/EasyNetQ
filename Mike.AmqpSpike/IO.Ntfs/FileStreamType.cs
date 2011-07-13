namespace Mike.AmqpSpike.IO.Ntfs
{
	/// <summary>
	/// Represents the type of data in a stream.
	/// </summary>
	public enum FileStreamType
	{
		/// <summary>
		/// Unknown stream type.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Standard data.
		/// </summary>
		Data = 1,
		/// <summary>
		/// Extended attribute data.
		/// </summary>
		ExtendedAttributes = 2,
		/// <summary>
		/// Security data.
		/// </summary>
		SecurityData = 3,
		/// <summary>
		/// Alternate data stream.
		/// </summary>
		AlternateDataStream = 4,
		/// <summary>
		/// Hard link information.
		/// </summary>
		Link = 5,
		/// <summary>
		/// Property data.
		/// </summary>
		PropertyData = 6,
		/// <summary>
		/// Object identifiers.
		/// </summary>
		ObjectId = 7,
		/// <summary>
		/// Reparse points.
		/// </summary>
		ReparseData = 8,
		/// <summary>
		/// Sparse file.
		/// </summary>
		SparseBlock = 9,
		/// <summary>
		/// Transactional data.
		/// (Undocumented - BACKUP_TXFS_DATA)
		/// </summary>
		TransactionData = 10,
	}
}

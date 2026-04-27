namespace UIWidgets.Examples
{
	using System;
	using UnityEngine;

	[Serializable]
	public class SimpleTableItem
	{
		[SerializeField]
		public string Field1; // RANK

		[SerializeField]
		public string Field2; // PLAYER

		[SerializeField]
		public string Field3; // RND

		[SerializeField]
		public string Field4; // TOTAL（追加）

		public override string ToString()
		{
			return string.Format("{0} | {1} | {2} | {3}", Field1, Field2, Field3, Field4);
		}
	}
}





//namespace UIWidgets.Examples
//{
//	using System;
//	using UIWidgets;
//	using UnityEngine;

//	/// <summary>
//	/// SimpleTable item.
//	/// </summary>
//	[Serializable]
//	public class SimpleTableItem
//	{
//		/// <summary>
//		/// Field1.
//		/// </summary>
//		[SerializeField]
//		public string Field1;

//		/// <summary>
//		/// Field2.
//		/// </summary>
//		[SerializeField]
//		public string Field2;

//		/// <summary>
//		/// Field3.
//		/// </summary>
//		[SerializeField]
//		public string Field3;

//		/// <summary>
//		/// Convert instance to string.
//		/// </summary>
//		/// <returns>String.</returns>
//		public override string ToString()
//		{
//			return string.Format("{0} | {1} | {2}", Field1, Field2, Field3);
//		}
//	}
//}
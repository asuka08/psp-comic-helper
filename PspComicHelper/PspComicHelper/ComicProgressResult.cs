namespace PspComicHelper
{
	/// <summary>
	/// ����������
	/// </summary>
	public enum ComicProgressResult
	{
		/// <summary>
		/// ���
		/// </summary>
		Complete = 1,

		/// <summary>
		/// ·������
		/// </summary>
		PathError = 2,

		/// <summary>
		/// ��֧�ֵ�ѹ����ʽ
		/// </summary>
		UnsupportedArchive = 3,

		/// <summary>
		/// Ŀ¼����ͼƬ
		/// </summary>
		NoneImageInFolder = 4
	}
}
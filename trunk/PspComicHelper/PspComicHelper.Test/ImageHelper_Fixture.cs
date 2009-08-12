using System.Drawing;
using NUnit.Framework;

namespace PspComicHelper.Test
{
	[TestFixture]
	public class ImageHelper_Fixture
	{
		/// <summary>
		/// ͼ���Ե��� ����
		/// </summary>
		[Test]
		public void DetectMargin_Test()
		{
			Bitmap bitmap = new Bitmap( @"resources\DetectMargin_sample1.jpg" );
			ImageMargin margin = ImageHelper.DetectMargin( bitmap, 225 );
			Assert.AreEqual( 2, margin.Left );
			Assert.AreEqual( 2, margin.Top );
			Assert.AreEqual( 2, margin.Right );
			Assert.AreEqual( 2, margin.Bottom );
			bitmap.Dispose();
		}

		[Test]
		public void CutMargin_Specify_Test()
		{
			Bitmap bitmap = new Bitmap( @"resources\DetectMargin_sample1.jpg" );
			Bitmap cutted = ImageHelper.CutMargin( bitmap, new ImageMargin{ Left = 1, Top = 1, Right = 2, Bottom = 2 } );
			Assert.AreEqual( 7, cutted.Width );
			Assert.AreEqual( 7, cutted.Height );
			ImageHelper.SaveAsJpeg( cutted, "output1.jpg", 89 );
			bitmap.Dispose();
			cutted.Dispose();
		}


		[Test]
		public void CutMargin_Auto_Test()
		{
			// �ڰ�ͼ
			Bitmap bitmap = new Bitmap( @"resources\DetectMargin_sample1.jpg" );
			Bitmap cutted = ImageHelper.CutMargin( bitmap, 200 );
			Assert.AreEqual( 6, cutted.Width );
			Assert.AreEqual( 6, cutted.Height );
			ImageHelper.SaveAsJpeg( cutted, "output2.jpg", 89 );
			bitmap.Dispose();
			cutted.Dispose();

			// ��ɫͼ
			bitmap = new Bitmap( @"resources\DetectMargin_sample2.jpg" );
			cutted = ImageHelper.CutMargin( bitmap, 225 );
			Assert.AreEqual( 6, cutted.Width );
			Assert.AreEqual( 6, cutted.Height );
			cutted.Save( "output3.bmp", System.Drawing.Imaging.ImageFormat.Bmp );
			//ImageHelper.SaveAsJpeg( cutted, "output3.jpg", 89 );
			bitmap.Dispose();
			cutted.Dispose();

			// �Ҷ�ͼ
			bitmap = new Bitmap( @"resources\DetectMargin_sample3.jpg" );
			cutted = ImageHelper.CutMargin( bitmap, 240 );
			Assert.AreEqual( 6, cutted.Width );
			Assert.AreEqual( 6, cutted.Height );
			cutted.Save( "output4.bmp", System.Drawing.Imaging.ImageFormat.Bmp );
			ImageHelper.SaveAsJpeg( cutted, "output4.jpg", 89 );
			bitmap.Dispose();
			cutted.Dispose();
		}

	}
}
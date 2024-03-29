﻿using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace PspComicHelper
{
	/// <summary>
	/// 图片处理类
	/// </summary>
	public class ImageHelper
	{
		/// <summary>
		/// 得到指定mimeType的ImageCodecInfo
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns>得到指定mimeType的ImageCodecInfo</returns>
        private static ImageCodecInfo GetCodecInfo(string mimeType)
        {
            ImageCodecInfo[] CodecInfo = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo ici in CodecInfo)
            {
                if (ici.MimeType == mimeType) return ici;
            }
            return null;
        }

		/// <summary>
        /// 保存为JPEG格式，支持压缩质量选项
        /// </summary>
        /// <param name="bmp"></param>
		/// <param name="filename"></param>
		/// <param name="quality"></param>
        /// <returns></returns>
        public static bool SaveAsJpeg(Bitmap bmp, string filename, int quality)
        {
            EncoderParameter p;
            EncoderParameters ps;

            ps = new EncoderParameters(1);

            p = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            ps.Param[0] = p;

            bmp.Save(filename, GetCodecInfo("image/jpeg"), ps);
            
            return true;

        }


		/// <summary>
		/// 调整大小
		/// </summary>
		/// <param name="bmp">原始Bitmap</param>
		/// <param name="width">新的宽度</param>
		/// <param name="height">新的高度</param>
		/// <param name="mode">保留着，暂时未用</param>
		/// <returns>处理以后的图片</returns>
		public static Bitmap Resize( Bitmap bmp, int width, int height, ResizeMode mode )
		{

			int newWidth, newHeight;
			CalcSize( bmp.Width, bmp.Height, width, height, out newWidth, out newHeight, mode );

			// 如果使用等比缩放模式, 新的宽,高大于原图的宽,高,不做操作
			if ( ( mode == ResizeMode.Scale ) && ( newWidth > bmp.Width || newHeight > bmp.Height ) )
			{
				return bmp;
			}
			if( ( mode == ResizeMode.Center ) && ( width > bmp.Width && height > bmp.Height ) )
			{
				return bmp;
			}

			Bitmap b = new Bitmap( newWidth, newHeight );
			Graphics g = Graphics.FromImage( b );

			// 插值算法的质量
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

			g.DrawImage( bmp, new Rectangle( 0, 0, newWidth, newHeight ), new Rectangle( 0, 0, bmp.Width, bmp.Height ), GraphicsUnit.Pixel );
			g.Dispose();

			// 如果是居中模式, 则需要剪裁掉多余的边
			if ( ( mode == ResizeMode.Center ) && ( ( newWidth != width || newHeight != height ) ) && ( width != 0 && height != 0 ) )
			{
				int startX, startY, cutWidth, cutHeight;
				startX = ( newWidth - width ) / 2;
				if( startX < 0 )
					startX = 0;
				startY = ( newHeight - height ) / 2;
				if( startY < 0 )
					startY = 0;
				cutWidth = ( width < newWidth ) ? width : newWidth;
				cutHeight = ( height < newHeight ) ? height : newHeight;

				b = Cut( b, startX, startY, cutWidth, cutHeight );
			}
			return b;
		}


		/// <summary>
		/// 剪裁
		/// </summary>
		/// <param name="bitmap">原始Bitmap</param>
		/// <param name="startX">开始坐标X</param>
		/// <param name="startY">开始坐标Y</param>
		/// <param name="width">宽度</param>
		/// <param name="height">高度</param>
		/// <returns>剪裁后的Bitmap</returns>
		public static Bitmap Cut( Bitmap bitmap, int startX, int startY, int width, int height )
		{
			if ( bitmap == null )
			{
				return null;
			}

			int w = bitmap.Width;
			int h = bitmap.Height;

			if ( startX >= w || startY >= h )
			{
				return null;
			}

			if ( startX + width > w )
			{
				width = w - startX;
			}

			if ( startY + height > h )
			{
				height = h - startY;
			}

			Bitmap bmpOut = new Bitmap( width, height, PixelFormat.Format24bppRgb );

			Graphics g = Graphics.FromImage( bmpOut );
			g.DrawImage( bitmap, new Rectangle( 0, 0, width, height ), new Rectangle( startX, startY, width, height ), GraphicsUnit.Pixel );
			g.Dispose();

			return bmpOut;
		}


		/// <summary>
		/// 检测图像边缘
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="threshold"></param>
		/// <returns></returns>
		public static ImageMargin DetectMargin( Bitmap bitmap, int threshold )
		{
			
			if ( threshold > 255 || threshold < 0 )
				threshold = 255;
			int left = -1 , top = -1, right = -1, bottom = -1;
			int brightness;

			// 检测左边缘
			for ( int x = 0; x < bitmap.Width; x++ )
			{
				for ( int y = 0; y < bitmap.Height; y++ )
				{
					brightness = (int)( bitmap.GetPixel( x, y ).GetBrightness() * 255 );
					if ( brightness <= threshold )
					{
						left = x;
						break;
					}
				}
				if( left >= 0 )
					break;
			}

			// 检测上边缘
			for ( int y = 0; y < bitmap.Height; y++ )
			{
				for ( int x = left; x < bitmap.Width; x++ )
				{
					brightness = (int)( bitmap.GetPixel( x, y ).GetBrightness() * 255 );
					if ( brightness <= threshold )
					{
						top = y;
						break;
					}
				}
				if( top >= 0 )
					break;
			}

			// 检测右边缘
			for ( int x = bitmap.Width - 1; x > left; x-- )
			{
				for ( int y = top; y < bitmap.Height; y++ )
				{
					brightness = (int)( bitmap.GetPixel( x, y ).GetBrightness() * 255 );
					if ( brightness <= threshold )
					{
						right = bitmap.Width - 1 - x;
						break;
					}
				}
				if( right >= 0 )
					break;
			}

			// 检测下边缘
			for ( int y = bitmap.Height - 1; y > top; y-- )
			{
				for ( int x = left; x < bitmap.Width - 1 - right; x++ )
				{
					brightness = (int)( bitmap.GetPixel( x, y ).GetBrightness() * 255 );
					if ( brightness <= threshold )
					{
						bottom = bitmap.Height - 1 - y;
						break;
					}
				}
				if( bottom >= 0 )
					break;
			}

			return new ImageMargin{ Left = left, Top = top, Right = right, Bottom = bottom };
		}


		/// <summary>
		/// 裁剪边缘 指定边缘
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="margin"></param>
		/// <returns></returns>
		public static Bitmap CutMargin( Bitmap bitmap, ImageMargin margin )
		{
			return Cut( bitmap, margin.Left, margin.Top,
				bitmap.Width - margin.Left - margin.Right,
				bitmap.Height - margin.Top - margin.Bottom );
		}

		/// <summary>
		/// 裁剪边缘 自动检测
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="threshold"></param>
		/// <returns></returns>
		public static Bitmap CutMargin( Bitmap bitmap, int threshold )
		{
			return CutMargin( bitmap, DetectMargin( bitmap, threshold ) );
		}


		/// <summary>
		/// 计算宽高
		/// </summary>
		public static void CalcSize( int sourceWidth, int sourceHeight, int destWidth, int destHeight, out int newWidth, out int newHeight, ResizeMode mode )
		{
			if( destWidth == 0 && destHeight == 0 )
			{
				newWidth = 0;
				newHeight = 0;
				return;
			}
			else if( destWidth == 0 )
			{
				newWidth = (int)( (float)destHeight * ( (float)sourceWidth / (float)sourceHeight ) );
				newHeight = destHeight;
				return;
			}
			else if( destHeight == 0 )
			{
				newWidth = destWidth;
				newHeight = (int)( (float)destWidth * ( (float)sourceHeight / (float)sourceWidth ) );
				return;
			}

			switch ( mode )
			{
				case ResizeMode.Stretch:
					newWidth = destWidth;
					newHeight = destHeight;
					break;

				case ResizeMode.Center:
					if ( ( (float)sourceWidth / (float)destWidth ) < ( (float)sourceHeight / (float)destHeight ) )
					{
						if( destWidth > sourceWidth )
							destWidth = sourceWidth;
						newWidth = destWidth;
						newHeight = sourceHeight * destWidth / sourceWidth;
					}
					else
					{
						if( destHeight > sourceHeight )
							destHeight = sourceHeight;
						newWidth = sourceWidth * destHeight / sourceHeight;
						newHeight = destHeight;
					}
					break;

				case ResizeMode.Scale:
				default:
					if ( ( (float)destWidth / (float)destHeight ) > ( (float)sourceWidth / (float)sourceHeight ) )
					{
						newWidth = (int)( (float)destHeight * ( (float)sourceWidth / (float)sourceHeight ) );
						newHeight = destHeight;
					}
					else
					{
						newWidth = destWidth;
						newHeight = (int)( (float)destWidth * ( (float)sourceHeight / (float)sourceWidth ) );
					}
					break;
			}
		}


		/// <summary>
		/// 图片缩放模式
		/// </summary>
		public enum ResizeMode
		{
			/// <summary>
			/// 等比
			/// </summary>
			Scale = 0,

			/// <summary>
			/// 拉伸
			/// </summary>
			Stretch = 1,

			/// <summary>
			/// 居中
			/// </summary>
			Center = 2
		}


	}
}

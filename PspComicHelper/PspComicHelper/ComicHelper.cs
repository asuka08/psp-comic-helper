using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;

namespace PspComicHelper
{
	/// <summary>
	/// 漫画文件处理类
	/// </summary>
	public class ComicHelper
	{
		private static readonly List<string> imageExts = new List<string>(){ ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
		private static Microsoft.VisualBasic.Devices.Computer _computer = new Microsoft.VisualBasic.Devices.Computer();
		private const string UNZIP_TEMP_FOLDER = "unzip_temp";

		public static int RateOfProgress { get; set; }

		/// <summary>
		/// 处理图片路径
		/// </summary>
		/// <param name="path"></param>
		public static ComicProgressResult ProgressComicPath( string path )
		{
			RateOfProgress = 0;

			// 创建临时目录
			DeleteDirectory(Setting.TempPath);
			Directory.CreateDirectory( Setting.TempPath );

			ComicProgressResult result;

			if ( File.Exists( path ) )
			{
				// 如果是文件, 调用处理压缩文档的方法
				result = ProgressComicArchive( path );
			}
			else if ( Directory.Exists( path ) )
			{
				// 如果是目录, 调用处理目录的方法
				result = ProgressComicFolder( path );
			}
			else
			{
				result = ComicProgressResult.PathError;
			}

			// 删除临时目录
			DeleteDirectory( Setting.TempPath );

			RateOfProgress = 100;

			return result;

		}

		/// <summary>
		/// 处理图片路径 压缩文档
		/// </summary>
		/// <param name="path"></param>
		private static ComicProgressResult ProgressComicArchive( string path )
		{
			RateOfProgress = 5;

			string unzipTempPath = Path.Combine( Setting.TempPath, UNZIP_TEMP_FOLDER );
			unzipTempPath = Path.Combine( unzipTempPath, Path.GetFileNameWithoutExtension( path ) );
			string ext = Path.GetExtension( path ).ToLower();
			ComicProgressResult result;
			if ( ext != ".zip" && ext != ".rar" && ext != ".cbr" && ext != ".cbz" )
			{
				return ComicProgressResult.UnsupportedArchive;
			}

			
			// 创建解压临时目录
			DeleteDirectory( unzipTempPath );
			Directory.CreateDirectory( unzipTempPath );


			if ( ext == ".zip" || ext == ".cbz" )
			{
				// 使用SharpZipLib解压缩zip
				FastZip fz = new FastZip();
				fz.CreateEmptyDirectories = true;
				fz.ExtractZip( path, unzipTempPath, "" );
				fz = null;
				result = ProgressComicFolder( unzipTempPath, true );
			}
			else if ( ext == ".rar" || ext == ".cbr" )
			{
				// 使用unrar.exe解压缩rar
				string args = string.Format( "x -o+ \"{0}\" \"{1}\\\"", path, unzipTempPath );
				using ( System.Diagnostics.Process rar = new System.Diagnostics.Process() )
				{
					rar.StartInfo.FileName = Path.Combine( Setting.AppPath, "rar.exe" );
					rar.StartInfo.Arguments = args;
					rar.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
					rar.Start();
					rar.WaitForExit();
					rar.Close();
				}
				result = ProgressComicFolder( unzipTempPath, true );
			}
			else
			{
				result = ComicProgressResult.UnsupportedArchive;
			}
			

			// 删除解压临时目录
			DeleteDirectory( unzipTempPath );

			return result;
		}

		/// <summary>
		/// 处理图片路径 文件夹
		/// </summary>
		/// <param name="path"></param>
		private static ComicProgressResult ProgressComicFolder( string path, bool includeSubdirectory )
		{
			RateOfProgress = 20;

			// 取得文件夹中所有图片文件
			string [] filesArr = Directory.GetFiles( path, "*.*", includeSubdirectory ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );
			Array.Sort( filesArr );

			List<string> files = new List<string>( filesArr );
			for ( int i = files.Count - 1; i >= 0; i-- )
			{
				if ( !imageExts.Contains( Path.GetExtension( files[i] ).ToLower() ) )
				{
					files.RemoveAt( i );
				}
			}

			if ( files.Count == 0 )
			{
				return ComicProgressResult.NoneImageInFolder;
			}

			// 输出文件夹
			string tempOutput = Path.Combine( Setting.TempPath, Path.GetFileName( path ) );
			string originalOutput = tempOutput;
			for ( int i = 1; Directory.Exists( tempOutput ); i++ )
			{
				// 如果文件夹重名, 加上数字
				tempOutput = originalOutput + string.Format( "({0})", i );
			}
			Directory.CreateDirectory( tempOutput );

			// 把图片缩放并存放到临时目录
			for ( int i = 0; i < files.Count; i++ )
			{
				ProgressComicFile( files[i], Path.Combine( tempOutput, i.ToString( "d6" ) + ".jpg" ) );
				//File.Copy( files[i], Path.Combine( output, Path.GetFileName( files[i] ) ) );
				RateOfProgress = 20 + 60 * i / files.Count;
				//新加入代码： 每处理20张图片就把最大内存占用量设为200MB, avoid crash on some machine - purplezhang@gmail.com
                if( i % 20 == 0 ){
                    GC.Collect();
                    System.Diagnostics.Process.GetCurrentProcess().MaxWorkingSet = (IntPtr)209715200;
                }
			}

			ProgressComicToOutput( tempOutput );
			RateOfProgress = 99;

			return ComicProgressResult.Complete;
		}

		private static ComicProgressResult ProgressComicFolder( string path )
		{
			return ProgressComicFolder( path, false );
		}

		/// <summary>
		/// 将临时目录中的漫画处理到输出目录
		/// </summary>
		private static void ProgressComicToOutput( string temp )
		{
			if ( Setting.OutputZip )
			{
				// 使用 SharpZipLib 生成压缩文档
				FastZip fz = new FastZip();
				fz.CreateEmptyDirectories = true;

				string zipfilePath = Path.Combine( Setting.OutputPath, Path.GetFileName( temp ) );
				string originalZipFilePath = zipfilePath;
				for ( int i = 1; File.Exists( zipfilePath + ".zip" ); i++ )
				{
					zipfilePath = originalZipFilePath + string.Format( "({0})", i );
				}
				zipfilePath = zipfilePath + ".zip";
				fz.CreateZip( zipfilePath, temp, false, "" );
				fz = null;
			}
			else
			{
				string outputPath = Path.Combine( Setting.OutputPath, Path.GetFileName( temp ) );
				string originalOutput = outputPath;
				for ( int i = 1; Directory.Exists( outputPath ); i++ )
				{
					outputPath = originalOutput + string.Format( "({0})", i );
				}
				_computer.FileSystem.CopyDirectory( temp, outputPath );
			}

		}

		/// <summary>
		/// 处理单张图片
		/// </summary>
		private static void ProgressComicFile( string source, string dest )
		{
			Bitmap bitmap = new Bitmap( source );
			if ( Setting.AutoCutMargin )
			{
				bitmap = ImageHelper.CutMargin( bitmap, Setting.Threshold );
			}
			if ( ( Setting.Width > 0 ) || ( Setting.Height > 0 ) )
			{
				if ( Setting.SplitTowPage && ( bitmap.Width > bitmap.Height ) )
				{
					Bitmap left = ImageHelper.Cut( bitmap, 0, 0, bitmap.Width / 2, bitmap.Height );
					Bitmap right = ImageHelper.Cut( bitmap, bitmap.Width / 2, 0, bitmap.Width / 2, bitmap.Height );
					left = ImageHelper.Resize( left, Setting.Width, Setting.Height, Setting.Mode );
					right = ImageHelper.Resize( right, Setting.Width, Setting.Height, Setting.Mode );

					if ( Setting.ReadOrder == ReadOrderEnum.RightToLeft )
					{
						ImageHelper.SaveAsJpeg( right, Regex.Replace( dest, "\\.jpg$", "a.jpg" ), Setting.Quality );
						ImageHelper.SaveAsJpeg( left, Regex.Replace( dest, "\\.jpg$", "b.jpg" ), Setting.Quality );
					}
					else
					{
						ImageHelper.SaveAsJpeg( left, Regex.Replace( dest, "\\.jpg$", "a.jpg" ), Setting.Quality );
						ImageHelper.SaveAsJpeg( right, Regex.Replace( dest, "\\.jpg$", "b.jpg" ), Setting.Quality );
					}
					left.Dispose();
					right.Dispose();
					bitmap.Dispose();

				}
				else
				{
					bitmap = ImageHelper.Resize( bitmap, Setting.Width, Setting.Height, Setting.Mode );
					ImageHelper.SaveAsJpeg( bitmap, dest, Setting.Quality );
					bitmap.Dispose();
				}
			}
			else
			{
				File.Copy( source, dest );
			}
		}


		/// <summary>
		/// 删除文件夹
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool DeleteDirectory( string path )
		{
			if ( Directory.Exists( path ) )
			{
				try
				{
					Directory.Delete( path, true );
				}
				catch ( IOException )
				{
					return false;
				}
				catch ( UnauthorizedAccessException )
				{
					foreach( string file in Directory.GetFiles( path, "*.*", SearchOption.AllDirectories ) )
					{
						FileInfo fi = new FileInfo(file);
						if( fi.IsReadOnly )
						{
							fi.Attributes = FileAttributes.Normal;
						}
						File.Delete(file);
					}
					try
					{
						Directory.Delete(path, true);
					}
					catch
					{
						return false;
					}

				}
			}
			return true;
		}


	}
}

﻿using System;
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

		/// <summary>
		/// 处理图片路径
		/// </summary>
		/// <param name="path"></param>
		public static string ProgressComicPath( string path )
		{
			// 创建临时目录
			if ( Directory.Exists( Setting.TempPath ) )
			{
				try
				{
					Directory.Delete( Setting.TempPath, true );
				}
				catch ( IOException )
				{
					// 删除临时目录失败
				}
			}
			Directory.CreateDirectory( Setting.TempPath );

			string result;

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
				result = "路径错误";
			}

			// 删除临时目录
			if ( Directory.Exists( Setting.TempPath ) )
			{
				try
				{
					Directory.Delete( Setting.TempPath, true );
				}
				catch ( IOException )
				{
					// 删除临时目录失败
				}
			}

			return result;

		}

		/// <summary>
		/// 处理图片路径 压缩文档
		/// </summary>
		/// <param name="path"></param>
		private static string ProgressComicArchive( string path )
		{

			string unzipTempPath = Path.Combine( Setting.TempPath, UNZIP_TEMP_FOLDER );
			unzipTempPath = Path.Combine( unzipTempPath, Path.GetFileNameWithoutExtension( path ) );
			string ext = Path.GetExtension( path ).ToLower();
			string result;
			if ( ext != ".zip" && ext != ".rar" )
			{
				return "不支持的压缩格式";
			}

			
			// 创建解压临时目录
			if ( Directory.Exists( unzipTempPath ) )
			{
				Directory.Delete( unzipTempPath );
			}
			Directory.CreateDirectory( unzipTempPath );

			if ( ext == ".zip" )
			{
				// 使用SharpZipLib解压缩zip
				FastZip fz = new FastZip();
				fz.CreateEmptyDirectories = true;
				fz.ExtractZip( path, unzipTempPath, "" );
				fz = null;
				result = ProgressComicFolder( unzipTempPath, true );
			}
			else if ( ext == ".rar" )
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
				result = "不支持的压缩格式";
			}
			

			// 删除解压临时目录
			if ( Directory.Exists( unzipTempPath ) )
			{
				try
				{
					Directory.Delete( unzipTempPath, true );
				}
				catch( IOException )
				{
					// 删除临时目录失败
				}
			}

			return result;
		}

		/// <summary>
		/// 处理图片路径 文件夹
		/// </summary>
		/// <param name="path"></param>
		private static string ProgressComicFolder( string path, bool includeSubdirectory )
		{
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
				return "目录内无图片";
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
			}

			ProgressComicToOutput( tempOutput );

			return "完成";
		}

		private static string ProgressComicFolder( string path )
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
		/// <param name="p"></param>
		/// <param name="p_2"></param>
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
					left = ImageHelper.Resize( left, Setting.Width, Setting.Height, ImageHelper.ResizeMode.Scale );
					right = ImageHelper.Resize( right, Setting.Width, Setting.Height, ImageHelper.ResizeMode.Scale );

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
					bitmap = ImageHelper.Resize( bitmap, Setting.Width, Setting.Height, ImageHelper.ResizeMode.Scale );
					ImageHelper.SaveAsJpeg( bitmap, dest, Setting.Quality );
					bitmap.Dispose();
				}
			}
			else
			{
				File.Copy( source, dest );
			}
		}


	}
}

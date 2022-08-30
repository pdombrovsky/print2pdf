using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SstPrint2Pdf.AcDrawing
{
    
    /// <summary>
	/// Provides easy access to several "active" objects in the AutoCAD
	/// runtime environment.
	/// </summary>
	public static class CurrentDrawing
	{
		/// <summary>
		/// Returns the active Editor object.
		/// </summary>
		public static Editor Editor
		{
			get { return Document.Editor; }
		}

		/// <summary>
		/// Returns the active Document object.
		/// </summary>
		public static Document Document
		{
			get { return Application.DocumentManager.MdiActiveDocument; }
		}

		/// <summary>
		/// Returns the active Database object.
		/// </summary>
		public static Database Database
		{
			get { return Document.Database; }
		}

		/// <summary>
		/// Get Path to current dwg not inclding it's name
		/// </summary>
		
		public static string Directory()
		{
            var tmp = Path.GetDirectoryName(Document.Name);
            
            return (String.IsNullOrWhiteSpace(tmp)) ? Path.GetTempPath() : tmp;
		}

		
	}
}

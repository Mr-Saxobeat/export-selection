using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace WBlock
{
    public static class wBlockEntity
    {
        [CommandMethod("wblockEntity")]
        public static void wblockEntity()
        {
            // Get the document, the database and the editor object
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var PrpSelOpts = new PromptSelectionOptions();
            PrpSelOpts.MessageForAdding = "Selecione os objetos: ";
            PromptSelectionResult prRes = ed.GetSelection(PrpSelOpts);

            if (prRes.Status != PromptStatus.OK)
                return;

            ObjectIdCollection objIds = new ObjectIdCollection();
            ObjectId[] objIdArray = prRes.Value.GetObjectIds();

            // Copy objectIds to objectIdCollection
            foreach (ObjectId id in objIdArray)
                objIds.Add(id);

            // Set the GetString method to get a new file name by user
            var pStrOpts = new PromptStringOptions("Digite o nome do novo documento: ");
            pStrOpts.AllowSpaces = true;
            PromptResult pStrRes = doc.Editor.GetString(pStrOpts);

            // Set the name of the new file will be created
            // in the same folder of the current file.
            string FileName = Application.GetSystemVariable("DWGPREFIX") + pStrRes.StringResult + ".dwg";

            // Create a new external database, where the
            // exported objects will be created.
            using (var newDb = new Database(true, false))
            {    
                db.Wblock(newDb, objIds, Point3d.Origin,
                                            DuplicateRecordCloning.Ignore);
                newDb.SaveAs(FileName, DwgVersion.Newest);
            }

            // Here the objects on the new database
            // will be moved to the origin point.
            using (var exDb = new Database(false, false))
            {
                try
                {
                    exDb.ReadDwgFile(FileName, FileOpenMode.OpenForReadAndWriteNoShare, false, "");
                }
                catch (System.Exception)
                {
                    ed.WriteMessage("\nUnable to read drawing file.");
                }

                using (var exTr = exDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table record for read
                    BlockTable exBlkTbl;
                    exBlkTbl = exTr.GetObject(exDb.BlockTableId,
                                                OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for read
                    BlockTableRecord exBlkTblRec;
                    exBlkTblRec = exTr.GetObject(exBlkTbl[BlockTableRecord.ModelSpace],
                                                    OpenMode.ForRead) as BlockTableRecord;
                }
            }
        }
    }
}

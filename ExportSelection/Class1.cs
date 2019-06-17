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

            PromptSelectionResult prRes = ed.GetSelection();

            if (prRes.Status != PromptStatus.OK)
                return;

            ObjectIdCollection objIds = new ObjectIdCollection();
            ObjectId[] objIdArray = prRes.Value.GetObjectIds();

            // Copy objectIds to objectIdCollection
            foreach (ObjectId id in objIdArray)
                objIds.Add(id);

            using (Database newDb = new Database(true, false))
            {
                db.Wblock(newDb, objIds, Point3d.Origin,
                                            DuplicateRecordCloning.Ignore);
                string FileName = db.Filename; // i need to change the name of the new file !!!!!!!!!!!!!!!W
                newDb.SaveAs(FileName, DwgVersion.Newest);
            }
        }
    }
}

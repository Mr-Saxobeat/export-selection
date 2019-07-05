using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace WBlock
{
    // A simple extension method that aggregates the extents of any entities
    // passed in (via their ObjectIds)
    // https://www.keanw.com/2015/07/getting-the-extents-of-an-autocad-group-using-net.html
    public static class TransactionExtensions
    {
        public static Extents3d GetExtents(this Transaction tr, ObjectId[] ids)
        {
            var ext = new Extents3d();
            foreach (var id in ids)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent != null)
                {
                    ext.AddExtents(ent.GeometricExtents);
                }
            }
            return ext;
        }
    }

    public static class Main
    {
        [CommandMethod("NOUT")]
        public static void ExportSelection()
        {
            // Get the document, the database and the editor object
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Prompt to user select objects
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

            // Prompt user to type a name to the new drawing
            var pStrOpts = new PromptStringOptions("\nDigite o nome do novo documento: ");
            pStrOpts.AllowSpaces = true;
            PromptResult pStrRes = doc.Editor.GetString(pStrOpts);

            if (pStrRes.Status != PromptStatus.OK)
                return;

            // Set the name of the new file will be created
            // in the same folder of the current file.
            string FileName = Application.GetSystemVariable("DWGPREFIX") + pStrRes.StringResult + ".dwg";

            using (var trMoveToOrigin = db.TransactionManager.StartTransaction())
            {
                // Get the extents points 
                // of the selected objects.
                var extPts = trMoveToOrigin.GetExtents(objIdArray);
                var minExPt = extPts.MinPoint;

                // Get vector from minimal extent point
                // to the origin point, that will be
                // used to move the selected objects.
                Vector3d acVec3d = minExPt.GetVectorTo(Point3d.Origin);

                // Move objects seleted to the origin point
                // based on the minimal point from your extents
                foreach (ObjectId objId in objIds)
                {
                    Entity e = trMoveToOrigin.GetObject(objId, OpenMode.ForWrite) as Entity;
                    e.TransformBy(Matrix3d.Displacement(acVec3d));
                }

                // Create a new external database, where the
                // exported objects will be created.
                using (var newDb = new Database(true, false))
                {
                    using (var trExport = db.TransactionManager.StartTransaction())
                    {
                        db.Wblock(newDb, objIds, Point3d.Origin,
                                            DuplicateRecordCloning.Ignore);
                        trExport.Commit();
                    }

                    // Records the original View to restore it at the end
                    ViewTableRecord originalViewTblRec = doc.Editor.GetCurrentView();

                    // Change the working database to the newDatabasse
                    HostApplicationServices.WorkingDatabase = newDb;

                    // Set the ZoomExtents 
                    Application.AcadApplication.GetType().InvokeMember("ZoomExtents",
                        System.Reflection.BindingFlags.InvokeMethod, null, Application.AcadApplication, null);

                    // Set the Grid variable to show the Grid
                    Application.SetSystemVariable("GRIDMODE", 1);
                    newDb.SaveAs(FileName, DwgVersion.Newest);

                    // Change the working database back to the original 
                    HostApplicationServices.WorkingDatabase = db;

                    ed.SetCurrentView(originalViewTblRec);
                    ed.Regen();
                }

                // Dispose of original drawing without commit,
                // because the objects need to be in your
                // original point at the end of the program.
                trMoveToOrigin.Dispose();
            }
        }
    }
}


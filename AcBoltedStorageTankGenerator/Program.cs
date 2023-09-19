using AcBoltedStorageTankGenereator.Models;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.IO;

namespace AcBoltedStorageTankGenerator
{
    public class Program : IExtensionApplication
    {
        readonly string dwgFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Panels.dwg");
        private void ImportBlocks()
        {
            DocumentCollection dm = Application.DocumentManager;
            Editor ed = dm.MdiActiveDocument.Editor;
            Database destDb = dm.MdiActiveDocument.Database;
            Database sourceDb = new Database(false, true);
            try
            {
                sourceDb.ReadDwgFile(dwgFilePath, FileShare.Read, true, "");

                ObjectIdCollection blockIds = new ObjectIdCollection();

                var tm = sourceDb.TransactionManager;

                using (var myT = tm.StartOpenCloseTransaction())
                {
                    BlockTable bt = (BlockTable)myT.GetObject(sourceDb.BlockTableId, OpenMode.ForRead, false);

                    foreach (ObjectId btrId in bt)
                    {
                        BlockTableRecord btr =
                        (BlockTableRecord)myT.GetObject(btrId, OpenMode.ForRead, false);

                        if (!btr.IsAnonymous && !btr.IsLayout)
                            blockIds.Add(btrId);
                        btr.Dispose();
                    }
                }
                var mapping = new IdMapping();
                sourceDb.WblockCloneObjects(blockIds,
                    destDb.BlockTableId,
                    mapping,
                    DuplicateRecordCloning.Replace,
                    false);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage("\nError during copy: " + ex.Message);
            }
            sourceDb.Dispose();
        }

        [CommandMethod("STGenerator")]
        public void StorageTankGeneratorCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            PromptPointResult ppr = editor.GetPoint("\nDesired insertion point: ");
            // Specify the insertion point for the block

            if (ppr.Status == PromptStatus.OK)
            {
                StorageTank storageTank = new StorageTank();

                doc.Editor.WriteMessage($"Generating Left/Right side.\n");
                PromptDoubleOptions panelsWidth = new PromptDoubleOptions("\nEnter the panel Height:");
                PromptDoubleOptions panelsHeight = new PromptDoubleOptions("\nEnter the panel Height:");
                PromptIntegerOptions numberOfColumns = new PromptIntegerOptions("\nEnter the number of Columns:");
                PromptIntegerOptions numberOfRows = new PromptIntegerOptions("\nEnter the number of Rows:");
                PromptStringOptions userInput = new PromptStringOptions("\nDo you want to continue? [Y]:");

                panelsWidth.DefaultValue = 1.22;
                numberOfColumns.DefaultValue = 1;
                numberOfRows.DefaultValue = 1;
                panelsHeight.DefaultValue = 1.22;
                userInput.DefaultValue = string.Empty;

                while (true)
                {
                    PromptResult r = editor.GetString(userInput);
                    if (r.StringResult != string.Empty || r.StringResult.ToUpper() == "Y")
                        break;

                    PromptDoubleResult panelHeight = editor.GetDouble(panelsHeight);
                    PromptDoubleResult panelWidth = editor.GetDouble(panelsWidth);
                    PromptIntegerResult columns = editor.GetInteger(numberOfColumns);
                    PromptIntegerResult rows = editor.GetInteger(numberOfRows);
                    
                    if (columns.Status != PromptStatus.OK || rows.Status != PromptStatus.OK)
                        return;
                    List<Panel> panels = new List<Panel>();
                    for (int column = 0; column < columns.Value; column++)
                    {
                       
                        panels.Add(new Panel() { Height = panelHeight.Value, Width = panelWidth.Value });
                    }
                    for (int i = 0; i < rows.Value; i++)
                    {
                        storageTank.LeftAndRight.Add(new RowModel() { Panels = panels });
                    }
                }


                // Specify the block name you want to insert
                string blockNameToInsert = "FlatBlock";
                Point3d insertionPoint = new Point3d(0,0,0);

                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    // Open the current space (ModelSpace or PaperSpace) for write
                    BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                        doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                    // Check if the block definition exists in the drawing
                    BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);

                    if (bt.Has(blockNameToInsert))
                    {
                        // Loop to insert multiple instances of the block on top of each other
                        for (int i = 0; i < storageTank.LeftAndRight.Count; i++) 
                        {
                            int count = 0;
                            foreach (var panel in storageTank.LeftAndRight[i].Panels)
                            {
                                Point3d currentInsertionPoint = new Point3d(
                                    insertionPoint.X + (count * panel.Width), insertionPoint.Y + (i * panel.Height), insertionPoint.Z);

                                // Create a new block reference
                                if (panel.Height == 1.22 && panel.Width == 1.22)
                                {
                                    BlockReference blockRef = new BlockReference(currentInsertionPoint, bt[blockNameToInsert]);
                                    // Add the block reference to the current space
                                    currentSpace.AppendEntity(blockRef);
                                    tr.AddNewlyCreatedDBObject(blockRef, true);
                                }
                                else
                                {
                                    // Create a rectangle with panel.Height and panel.Width next to the added object
                                    Point3d corner1 = currentInsertionPoint;
                                    Point3d corner2 = new Point3d(
                                        currentInsertionPoint.X + panel.Width, currentInsertionPoint.Y + panel.Height, currentInsertionPoint.Z);

                                    // Create a rectangle using a Polyline
                                    using (Polyline rectangle = new Polyline())
                                    {
                                        rectangle.AddVertexAt(0, new Point2d(corner1.X, corner1.Y), 0, 0, 0);
                                        rectangle.AddVertexAt(1, new Point2d(corner2.X, corner1.Y), 0, 0, 0);
                                        rectangle.AddVertexAt(2, new Point2d(corner2.X, corner2.Y), 0, 0, 0);
                                        rectangle.AddVertexAt(3, new Point2d(corner1.X, corner2.Y), 0, 0, 0);
                                        rectangle.Closed = true;

                                        // Add the rectangle to the current space
                                        currentSpace.AppendEntity(rectangle);
                                        tr.AddNewlyCreatedDBObject(rectangle, true);
                                    }
                                }
                                count++;
                            }
                        }
                        // Commit the transaction to save the changes
                        tr.Commit();
                    }
                    else
                    {
                        doc.Editor.WriteMessage($"Block '{blockNameToInsert}' does not exist in the drawing.");
                    }

                }
            }
        }



        public void Initialize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            double newExtendValue = 0.05;

            byte red = 242;
            byte green = 113;
            byte blue = 114;

            Color newColor = Color.FromRgb(red, green, blue);
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {

                Application.SetSystemVariable("DIMEXO", newExtendValue);
                DimStyleTableRecord dimStyle = new DimStyleTableRecord();
                dimStyle = tr.GetObject(doc.Database.Dimstyle, OpenMode.ForWrite) as DimStyleTableRecord;

                dimStyle.Dimclrd = newColor;

                tr.Commit();
            }

            ImportBlocks();
        }

        public void Terminate()
        {
            // Clean up resources
        }
    }
}

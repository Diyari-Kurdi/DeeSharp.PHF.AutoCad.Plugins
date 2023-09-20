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


        #region
        /*
        doc.Editor.WriteMessage($"Generating Left/Right side.\n");
        PromptDoubleOptions panelsWidth = new PromptDoubleOptions("\nEnter the panel Width:");
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
                var row = new RowModel() { Panels = panels };
                storageTank.LeftAndRight.Add(row);
            }
        }
        */
        #endregion


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
                storageTank.LeftAndRight.Add(new RowModel()
                {
                    Panels = new List<Panel>()
                    {
                        new Panel()
                        {
                            Height = 1.22,
                            Width = 1.22,
                        }
                    }
                });
                storageTank.LeftAndRight.Add(new RowModel()
                {
                    Panels = new List<Panel>()
                    {
                        new Panel()
                        {
                            Height = 1.22,
                            Width = 1.22,
                        }
                    }
                });
                storageTank.LeftAndRight.Add(new RowModel()
                {
                    Panels = new List<Panel>()
                    {
                        new Panel()
                        {
                            Height = 1.22,
                            Width = 0.61,
                        }
                    }
                });
                storageTank.LeftAndRight.Add(new RowModel()
                {
                    Panels = new List<Panel>()
                    {
                        new Panel()
                        {
                            Height = 1.22,
                            Width = 0.61,
                        }
                    }
                });


                // Specify the block name you want to insert
                string blockNameToInsert = "FlatBlock";
                Point3d insertionPoint = ppr.Value;

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
                        for (int rowIndex = 0; rowIndex < storageTank.LeftAndRight.Count; rowIndex++)
                        {
                            int rowCount = 0;
                            foreach (var panel in storageTank.LeftAndRight[rowIndex].Panels)
                            {
                               
                                // Create a new block reference
                                if (panel.Height == 1.22 && panel.Width == 1.22)
                                {
                                    Point3d currentInsertionPoint = new Point3d(
                                   insertionPoint.X + (rowCount * panel.Width), insertionPoint.Y + (rowIndex * panel.Height), insertionPoint.Z);

                                    BlockReference blockRef = new BlockReference(currentInsertionPoint, bt[blockNameToInsert]);
                                    // Add the block reference to the current space
                                    currentSpace.AppendEntity(blockRef);
                                    tr.AddNewlyCreatedDBObject(blockRef, true);
                                }
                                else
                                {
                                    Point3d currentInsertionPoint = new Point3d(
                                    insertionPoint.X + (rowCount * panel.Width), insertionPoint.Y - (rowIndex * panel.Height), insertionPoint.Z);
                                    // Create a rectangle with panel.Height and panel.Width next to the added object
                                    Point3d corner1 = currentInsertionPoint;
                                    Point3d corner2;

                                    if (panel.Width == 1.22)
                                    {
                                        corner2 = new Point3d(
                                            currentInsertionPoint.X + (panel.Width * rowCount), currentInsertionPoint.Y + (panel.Height * rowIndex), corner1.Z);
                                    }
                                    else
                                    {
                                        corner2 = new Point3d(
                                            currentInsertionPoint.X - (panel.Width * (rowCount + 1)), currentInsertionPoint.Y - (panel.Height * rowIndex), corner1.Z);
                                    }

                                    // Set the height to a fixed value of 1.22
                                    double adjustedHeight = 1.22;

                                    // Create a rectangle using a Polyline
                                    using (Polyline rectangle = new Polyline())
                                    {
                                        rectangle.AddVertexAt(0, new Point2d(corner1.X, corner1.Y), 0, 0, 0);
                                        rectangle.AddVertexAt(1, new Point2d(corner2.X, corner1.Y), 0, 0, 0);
                                        rectangle.AddVertexAt(2, new Point2d(corner2.X, corner1.Y - adjustedHeight), 0, 0, 0);
                                        rectangle.AddVertexAt(3, new Point2d(corner1.X, corner1.Y - adjustedHeight), 0, 0, 0);
                                        rectangle.Closed = true;

                                        // Add the rectangle to the current space
                                        currentSpace.AppendEntity(rectangle);
                                        tr.AddNewlyCreatedDBObject(rectangle, true);
                                    }
                                }
                                rowCount++;
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

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

            if (ppr.Status != PromptStatus.OK)
                return;
            StorageTank storageTank = new StorageTank();//GetUserInput(doc, editor);

            storageTank.LeftAndRight.Add(new RowModel()
            {
                Panels = new List<Panel>()
                        {
                            new Panel()
                            {
                                Height = 1220,
                                Width = 1220,
                            },
                            new Panel()
                            {
                                Height = 1220,
                                Width = 1220,
                            }
                        }
            });
            storageTank.LeftAndRight.Add(new RowModel()
            {
                Panels = new List<Panel>()
                        {
                            new Panel()
                            {
                                Height = 1220,
                                Width = 1220,
                            },
                            new Panel()
                            {
                                Height = 1220,
                                Width = 1220,
                            }
                        }
            });


            storageTank.FrontAndBack.Add(new RowModel()
            {
                Panels = new List<Panel>()
                        {
                            new Panel()
                            {
                                Height = 1220,
                                Width = 1220,
                            },
                            new Panel()
                            {
                                Height = 1220,
                                Width = 1220,
                            }
                        }
            });
            storageTank.FrontAndBack.Add(new RowModel()
            {
                Panels = new List<Panel>()
                        {
                            new Panel()
                            {
                                Height = 1220,
                                Width = 1220,
                            },
                            new Panel()
                            {
                                Height = 1220,
                                Width = 1220,
                            }
                        }
            });

            Point3d insertionPoint = ppr.Value;

            BuildSideViews(doc, storageTank, insertionPoint, "A - View");
            Point3d FrontInsertionPoint = new Point3d(insertionPoint.X + (storageTank.Length * 2), insertionPoint.Y, insertionPoint.Z);
            BuildSideViews(doc, storageTank, FrontInsertionPoint, "B - View", true);
            Point3d InnerInsertionPoint = new Point3d(insertionPoint.X, insertionPoint.Y - 2440, insertionPoint.Z);
            BuildInnerView(doc, storageTank, InnerInsertionPoint);

            Point3d ThreeDInsertionPoint = new Point3d(insertionPoint.X + 3660, insertionPoint.Y - 3660, insertionPoint.Z);
            Build3DView(doc, storageTank, ThreeDInsertionPoint, "A", false);
        }


        private StorageTank GetUserInput(Document doc, Editor editor)
        {
            StorageTank storageTank = new StorageTank();
            PromptDoubleOptions panelsWidth = new PromptDoubleOptions("\nEnter the panel Width:");
            PromptDoubleOptions panelsHeight = new PromptDoubleOptions("\nEnter the panel Height:");
            PromptIntegerOptions numberOfLeftColumns = new PromptIntegerOptions("\nEnter the number of Columns (Length):");
            PromptIntegerOptions numberOfFrontColumns = new PromptIntegerOptions("\nEnter the number of Columns (Width):");
            PromptIntegerOptions numberOfRows = new PromptIntegerOptions("\nEnter the number of Rows:");
            PromptStringOptions userInput = new PromptStringOptions("\nDo you want to continue? [Y]:");
            panelsWidth.DefaultValue = 1220;
            numberOfLeftColumns.DefaultValue = 1;
            numberOfFrontColumns.DefaultValue = 1;
            numberOfRows.DefaultValue = 1;
            panelsHeight.DefaultValue = 1220;
            userInput.DefaultValue = string.Empty;

            PromptIntegerResult leftColumns = editor.GetInteger(numberOfLeftColumns);
            PromptIntegerResult frontColumns = editor.GetInteger(numberOfFrontColumns);

            PromptIntegerResult rows = editor.GetInteger(numberOfRows);

            for (int i = 0; i < rows.Value; i++)
            {
                RowModel row = new RowModel();
                for (int column = 0; column < leftColumns.Value; column++)
                {
                    row.Panels.Add(new Panel());
                }
                storageTank.LeftAndRight.Add(row);
            }
            for (int i = 0; i < rows.Value; i++)
            {
                RowModel row = new RowModel();
                for (int column = 0; column < frontColumns.Value; column++)
                {
                    row.Panels.Add(new Panel());
                }
                storageTank.FrontAndBack.Add(row);
            }

            //for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            //{
            //    if (sideIndex == 0)
            //        doc.Editor.WriteMessage($"Generating Left/Right side.\n");
            //    else
            //        doc.Editor.WriteMessage($"Generating Front/Back side.\n");

            //    while (true)
            //    {
            //        PromptResult r = editor.GetString(userInput);
            //        if (r.StringResult != string.Empty || r.StringResult.ToUpper() == "Y")
            //            break;

            //        PromptDoubleResult panelHeight = editor.GetDouble(panelsHeight);
            //        PromptDoubleResult panelWidth = editor.GetDouble(panelsWidth);

            //    }
            //}

            return storageTank;
        }

        /*
                    storageTank.LeftAndRight.Add(new RowModel()
                    {
                        Panels = new List<Panel>()
                        {
                            new Panel()
                            {
                                Height = 1.22,
                                Width = 1.22,
                            },
                            new Panel()
                            {
                                Height = 1.22,
                                Width = 0.610,
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
                            },
                            new Panel()
                            {
                                Height = 1.22,
                                Width = 0.610,
                            }
                        }
                    });


                    storageTank.FrontAndBack.Add(new RowModel()
                    {
                        Panels = new List<Panel>()
                        {
                            new Panel()
                            {
                                Height = 1.22,
                                Width = 1.22,
                            },
                            new Panel()
                            {
                                Height = 1.22,
                                Width = 0.610,
                            }
                        }
                    });
                    storageTank.FrontAndBack.Add(new RowModel()
                    {
                        Panels = new List<Panel>()
                        {
                            new Panel()
                            {
                                Height = 1.22,
                                Width = 1.22,
                            },
                            new Panel()
                            {
                                Height = 1.22,
                                Width = 0.610,
                            }
                        }
                    });
                    */
        private void BuildSideViews(Document doc, StorageTank storageTank, Point3d insertionPoint, string view, bool isFront = false)
        {
            string blockName = "FlatBlock";
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                // Open the current space (ModelSpace or PaperSpace) for write
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                // Check if the block definition exists in the drawing
                BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                int rowsNum = 0;
                List<RowModel> rows = new List<RowModel>();

                if (isFront)
                {
                    rowsNum = storageTank.FrontAndBack.Count;
                    rows = storageTank.FrontAndBack;
                }
                else
                {
                    rowsNum = storageTank.LeftAndRight.Count;
                    rows = storageTank.LeftAndRight;
                }

                // Loop to insert multiple instances of the block on top of each other
                for (int rowIndex = 0; rowIndex < storageTank.LeftAndRight.Count; rowIndex++)
                {
                    int rowCount = 0;
                    var panelsCount = rows[rowIndex].Panels.Count;
                    foreach (var panel in rows[rowIndex].Panels)
                    {
                        // Create a new block reference
                        if (panel.Height == 1220 && panel.Width == 1220)
                        {
                            Point3d currentInsertionPoint = new Point3d(
                                insertionPoint.X + (rowCount * panel.Width), insertionPoint.Y + (rowIndex * panel.Height), insertionPoint.Z);

                            BlockReference blockRef = new BlockReference(currentInsertionPoint, bt[blockName]);
                            // Add the block reference to the current space
                            currentSpace.AppendEntity(blockRef);
                            tr.AddNewlyCreatedDBObject(blockRef, true);
                        }
                        else
                        {
                            Point3d currentInsertionPoint = new Point3d(
                                insertionPoint.X, insertionPoint.Y + ((rowIndex + 1) * panel.Height), insertionPoint.Z);
                            // Create a rectangle with panel.Height and panel.Width next to the added object
                            Point3d corner1 = currentInsertionPoint;
                            Point3d corner2;


                            if (panel.Width == 12200)
                            {
                                corner2 = new Point3d(
                                    currentInsertionPoint.X - panel.Width, currentInsertionPoint.Y + (panel.Height * rowIndex), corner1.Z);
                            }
                            else
                            {
                                corner2 = new Point3d(
                                    currentInsertionPoint.X - panel.Width, currentInsertionPoint.Y - (panel.Height * rowIndex), corner1.Z);
                            }

                            // Create a rectangle using a Polyline
                            using (Polyline rectangle = new Polyline())
                            {
                                rectangle.AddVertexAt(0, new Point2d(corner1.X, corner1.Y), 0, 0, 0);
                                rectangle.AddVertexAt(1, new Point2d(corner2.X, corner1.Y), 0, 0, 0);
                                rectangle.AddVertexAt(2, new Point2d(corner2.X, corner1.Y - panel.Height), 0, 0, 0);
                                rectangle.AddVertexAt(3, new Point2d(corner1.X, corner1.Y - panel.Height), 0, 0, 0);
                                rectangle.Closed = true;

                                // Add the rectangle to the current space
                                currentSpace.AppendEntity(rectangle);
                                tr.AddNewlyCreatedDBObject(rectangle, true);
                            }
                        }
                        rowCount++;
                    }
                }


                DBText text = new DBText
                {
                    Position = new Point3d(insertionPoint.X, insertionPoint.Y - 610, insertionPoint.Z),
                    Height = 180,
                    TextString = view
                };
                currentSpace.AppendEntity(text);
                tr.AddNewlyCreatedDBObject(text, true);

                // Commit the transaction to save the changes
                tr.Commit();

            }
        }

        private void BuildInnerView(Document doc, StorageTank storageTank, Point3d insertionPoint)
        {
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                // Create a rectangle with panel.Height and panel.Width next to the added object
                Point3d corner1 = insertionPoint;
                Point3d corner2 = new Point3d(storageTank.Width, storageTank.Length, 0);

                // Create a rectangle using a Polyline
                using (Polyline rectangle = new Polyline())
                {
                    rectangle.AddVertexAt(0, new Point2d(corner1.X, corner1.Y), 0, 0, 0);
                    rectangle.AddVertexAt(1, new Point2d(corner2.X, corner1.Y), 0, 0, 0);
                    rectangle.AddVertexAt(2, new Point2d(corner2.X, corner1.Y - storageTank.Width), 0, 0, 0);
                    rectangle.AddVertexAt(3, new Point2d(corner1.X, corner1.Y - storageTank.Width), 0, 0, 0);
                    rectangle.Closed = true;

                    // Add the rectangle to the current space
                    currentSpace.AppendEntity(rectangle);
                    tr.AddNewlyCreatedDBObject(rectangle, true);

                    DBText text = new DBText
                    {
                        Position = new Point3d(insertionPoint.X, rectangle.GetPoint2dAt(3).Y - 610, insertionPoint.Z),
                        Height = 180,
                        TextString = "C - View"
                    };

                    currentSpace.AppendEntity(text);
                    tr.AddNewlyCreatedDBObject(text, true);
                }
                // Commit the transaction to save the changes
                tr.Commit();

            }
        }

        private void Build3DView(Document doc, StorageTank storageTank, Point3d insertionPoint, string view, bool isFront)
        {
            string blockName = "LeftBlock";
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                // Open the current space (ModelSpace or PaperSpace) for write
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                // Check if the block definition exists in the drawing
                BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                int rowsNum = 0;
                List<RowModel> rows = new List<RowModel>();

                if (isFront)
                {
                    rowsNum = storageTank.FrontAndBack.Count;
                    rows = storageTank.FrontAndBack;
                }
                else
                {
                    rowsNum = storageTank.LeftAndRight.Count;
                    rows = storageTank.LeftAndRight;
                }

                // Loop to insert multiple instances of the block on top of each other
                for (int rowIndex = 0; rowIndex < storageTank.LeftAndRight.Count; rowIndex++)
                {
                    int rowCount = 0;
                    var panelsCount = rows[rowIndex].Panels.Count;
                    foreach (var panel in rows[rowIndex].Panels)
                    {
                        // Create a new block reference
                        if (panel.Height == 1220 && panel.Width == 1220)
                        {
                            Point3d currentInsertionPoint = new Point3d(
                                insertionPoint.X + (rowCount * panel.Width), insertionPoint.Y + (rowIndex * panel.Height), insertionPoint.Z);
                            if (rowIndex == 0)
                                currentInsertionPoint = new Point3d(currentInsertionPoint.X, currentInsertionPoint.Y + 285.0512, currentInsertionPoint.Z);
                            if (rowCount == 0)
                                currentInsertionPoint = new Point3d(currentInsertionPoint.X + 411.3601, currentInsertionPoint.Y + 467.3952, currentInsertionPoint.Z);
                            BlockReference blockRef = new BlockReference(currentInsertionPoint, bt[blockName]);
                            // Add the block reference to the current space
                            currentSpace.AppendEntity(blockRef);
                            tr.AddNewlyCreatedDBObject(blockRef, true);
                        }
                        else
                        {
                            Point3d currentInsertionPoint = new Point3d(
                                insertionPoint.X, insertionPoint.Y + ((rowIndex + 1) * panel.Height), insertionPoint.Z);
                            // Create a rectangle with panel.Height and panel.Width next to the added object
                            Point3d corner1 = currentInsertionPoint;
                            Point3d corner2;


                            if (panel.Width == 1220)
                            {
                                corner2 = new Point3d(
                                    currentInsertionPoint.X - panel.Width, currentInsertionPoint.Y + (panel.Height * rowIndex), corner1.Z);
                            }
                            else
                            {
                                corner2 = new Point3d(
                                    currentInsertionPoint.X - panel.Width, currentInsertionPoint.Y - (panel.Height * rowIndex), corner1.Z);
                            }

                            // Create a rectangle using a Polyline
                            using (Polyline rectangle = new Polyline())
                            {
                                rectangle.AddVertexAt(0, new Point2d(corner1.X, corner1.Y), 0, 0, 0);
                                rectangle.AddVertexAt(1, new Point2d(corner2.X, corner1.Y), 0, 0, 0);
                                rectangle.AddVertexAt(2, new Point2d(corner2.X, corner1.Y - panel.Height), 0, 0, 0);
                                rectangle.AddVertexAt(3, new Point2d(corner1.X, corner1.Y - panel.Height), 0, 0, 0);
                                rectangle.Closed = true;

                                // Add the rectangle to the current space
                                currentSpace.AppendEntity(rectangle);
                                tr.AddNewlyCreatedDBObject(rectangle, true);
                            }
                        }
                        rowCount++;
                    }
                }


                DBText text = new DBText
                {
                    Position = new Point3d(insertionPoint.X, insertionPoint.Y - 610, insertionPoint.Z),
                    Height = 180,
                    TextString = view
                };
                currentSpace.AppendEntity(text);
                tr.AddNewlyCreatedDBObject(text, true);

                // Commit the transaction to save the changes
                tr.Commit();

            }
        }

        public void Initialize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            double newExtendValue = 5;

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

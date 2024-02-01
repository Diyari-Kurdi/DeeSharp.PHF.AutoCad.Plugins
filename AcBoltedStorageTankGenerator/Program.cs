using AcBoltedStorageTankGenerator.Models;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
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
            catch (System.Exception ex)
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
            StorageTank storageTank = GetUserInput(editor); //new StorageTank();
            if (storageTank is null)
                return;
            Point3d insertionPoint = new Point3d(0, 0, 0);

            BuildSideViews(doc, storageTank, new Point3d(insertionPoint.X + storageTank.Length + 2440, insertionPoint.Y + storageTank.Width + 2440, insertionPoint.Z), "C - View");
            BuildSideViews(doc, storageTank, new Point3d(insertionPoint.X + storageTank.Length + 2440, insertionPoint.Y + insertionPoint.Y + (storageTank.Width + storageTank.Height * 2) + 2440, insertionPoint.Z), "A - View");

            BuildSideViews(doc, storageTank, new Point3d(insertionPoint.X, insertionPoint.Y + storageTank.Width + 2440, insertionPoint.Z), "D - View", true);
            BuildSideViews(doc, storageTank, new Point3d(insertionPoint.X, insertionPoint.Y + (storageTank.Width + storageTank.Height * 2) + 2440, insertionPoint.Z), "B - View", true);

            BuildTopBottomView(doc, storageTank, new Point3d(insertionPoint.X, insertionPoint.Y, insertionPoint.Z), "Top - View");
            BuildTopBottomView(doc, storageTank, new Point3d(insertionPoint.X + storageTank.Length + 2440, insertionPoint.Y, insertionPoint.Z), "Under - View");
            // Create a rectangle with panel.Height and panel.Width next to the added object


            BuildInnerView(doc, storageTank, new Point3d(insertionPoint.X + (storageTank.Length * 2) + 4880, insertionPoint.Y, insertionPoint.Z));

            BuildLeftAndRightInnerTriangleSupports(doc, storageTank, new Point3d(insertionPoint.X + (storageTank.Length * 2) + 4880, insertionPoint.Y, insertionPoint.Z));
            BuildTopAndDownInnerTriangleSupports(doc, storageTank, new Point3d(insertionPoint.X + (storageTank.Length * 2) + 4880, insertionPoint.Y, insertionPoint.Z));

            BuildLeftAndRightInnerSupports(doc, storageTank, new Point3d(insertionPoint.X + (storageTank.Length * 2) + 4880, insertionPoint.Y, insertionPoint.Z));
            BuildTopAndDownInnerSupports(doc, storageTank, new Point3d(insertionPoint.X + (storageTank.Length * 2) + 4880, insertionPoint.Y, insertionPoint.Z));

            ////A
            Point3d ThreeDInsertionPoint = new Point3d(insertionPoint.X + storageTank.Width + storageTank.Length + 4880, insertionPoint.Y + (storageTank.Width / 1220 * 609.9504) + (storageTank.Width + 2440), insertionPoint.Z);
            Build3DView(doc, storageTank, ThreeDInsertionPoint, "A - View", false);

            ////B
            Point3d ThreeDFrontInsertionPoint = new Point3d(insertionPoint.X + (((storageTank.Width / 1220) - 1) * 1056.5796) + storageTank.Width + storageTank.Length + 4880, insertionPoint.Y + (storageTank.Width + 2440), insertionPoint.Z);
            Build3DView(doc, storageTank, ThreeDFrontInsertionPoint, "B - View", true);

            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                // Define 3 points for the polyline

                // Position X  Position Y
                // 29922.3756  15249.8597
                // 23582.8980  18909.5621
                // 18300.0000  15859.8101

                Point2d currentInsertionPoint = new Point2d(insertionPoint.X + storageTank.Width + storageTank.Length + 4880, insertionPoint.Y + (storageTank.Width + 2440 + storageTank.Height));

                Point2d point1 = new Point2d(currentInsertionPoint.X, currentInsertionPoint.Y + (storageTank.Width / 1220) * 609.9504);
                Point2d point2 = new Point2d(currentInsertionPoint.X + ((storageTank.Length / 1220) * 1056.5796), currentInsertionPoint.Y + (((storageTank.Width + storageTank.Length) / 1220) * 609.9504));
                Point2d point3 = new Point2d(currentInsertionPoint.X + (((storageTank.Length + storageTank.Width) / 1220) * 1056.5796), currentInsertionPoint.Y + (storageTank.Length / 1220) * 609.9504);

                // Create the polyline
                Polyline polyline = new Polyline();
                polyline.AddVertexAt(0, point1, 0, 0, 0);
                polyline.AddVertexAt(1, point2, 0, 0, 0);
                polyline.AddVertexAt(2, point3, 0, 0, 0);
                // Define the rotation angle (180 degrees)
                //Point3d centroid = polyline.GeometricExtents.MinPoint + ((polyline.GeometricExtents.MaxPoint - polyline.GeometricExtents.MinPoint) / 2.0);
                //Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, new Point3d(point1.X, point1.Y, insertionPoint.Z));
                //polyline.TransformBy(rotationMatrix);
                //Matrix3d moveMatrix = Matrix3d.Displacement(new Vector3d((((storageTank.Length / 1220) + (storageTank.Width / 1220)) * 1056.5796) + storageTank.Width + storageTank.Length + 4880,
                //    ((storageTank.Length / 1220) - (storageTank.Width / 1220)) * 609.9504, 0));
                //polyline.TransformBy(moveMatrix);

                currentSpace.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline, true);
                tr.Commit();
            }

        }


        private StorageTank GetUserInput(Editor editor)
        {
            StorageTank storageTank = new StorageTank();
            PromptDoubleOptions panelsWidth = new PromptDoubleOptions("\nEnter the panel Width:");
            PromptDoubleOptions panelsHeight = new PromptDoubleOptions("\nEnter the panel Height:");
            PromptIntegerOptions numberOfLeftColumns = new PromptIntegerOptions("\nEnter the number of Columns (Length):");
            PromptIntegerOptions numberOfFrontColumns = new PromptIntegerOptions("\nEnter the number of Columns (Width):");
            PromptIntegerOptions numberOfRows = new PromptIntegerOptions("\nEnter the number of Rows:");
            PromptStringOptions userInput = new PromptStringOptions("\nDo you want to continue? [Y]:");
            panelsWidth.DefaultValue = 1220;
            numberOfLeftColumns.DefaultValue = 5;
            numberOfFrontColumns.DefaultValue = 6;
            numberOfRows.DefaultValue = 2;
            panelsHeight.DefaultValue = 1220;
            userInput.DefaultValue = string.Empty;

            PromptIntegerResult frontColumns = editor.GetInteger(numberOfLeftColumns);
            if (frontColumns.Status != PromptStatus.OK || frontColumns.Value <= 0)
                return null;
            PromptIntegerResult leftColumns = editor.GetInteger(numberOfFrontColumns);
            if (leftColumns.Status != PromptStatus.OK || leftColumns.Value <= 0)
                return null;
            PromptIntegerResult rows = editor.GetInteger(numberOfRows);
            if (rows.Status != PromptStatus.OK || rows.Value <= 0)
                return null;
            for (int i = 0; i < rows.Value; i++)
            {
                RowModel row = new RowModel();
                for (int column = 0; column < frontColumns.Value; column++)
                {
                    row.Panels.Add(new Panel());
                }
                storageTank.FrontAndBack.Add(row);
            }
            for (int i = 0; i < rows.Value; i++)
            {
                RowModel row = new RowModel();
                for (int column = 0; column < leftColumns.Value; column++)
                {
                    row.Panels.Add(new Panel());
                }
                storageTank.LeftAndRight.Add(row);
            }
            return storageTank;
        }

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

                using (var dim = new AlignedDimension(insertionPoint + new Vector3d(0.0, storageTank.Height, 0.0), insertionPoint + new Vector3d(0.0, 0, 0.0), insertionPoint + new Vector3d(-1000, 0.0, 0.0), string.Empty, doc.Database.Dimstyle))
                {
                    currentSpace.AppendEntity(dim);
                    tr.AddNewlyCreatedDBObject(dim, true);
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

        private void BuildTopBottomView(Document doc, StorageTank storageTank, Point3d insertionPoint, string view)
        {
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                // Create a rectangle with panel.Height and panel.Width next to the added object
                Point3d corner2 = new Point3d(storageTank.Length, storageTank.Width, 0);

                // Create a rectangle using a Polyline
                using (Polyline rectangle = new Polyline())
                {
                    rectangle.AddVertexAt(0, new Point2d(insertionPoint.X, insertionPoint.Y), 0, 0, 0);
                    rectangle.AddVertexAt(1, new Point2d(insertionPoint.X + corner2.X, insertionPoint.Y), 0, 0, 0);

                    rectangle.AddVertexAt(2, new Point2d(insertionPoint.X + corner2.X, insertionPoint.Y + corner2.Y), 0, 0, 0);
                    rectangle.AddVertexAt(3, new Point2d(insertionPoint.X, insertionPoint.Y + corner2.Y), 0, 0, 0);
                    rectangle.Closed = true;

                    // Add the rectangle to the current space
                    currentSpace.AppendEntity(rectangle);
                    tr.AddNewlyCreatedDBObject(rectangle, true);

                    DBText text = new DBText
                    {
                        Position = new Point3d(insertionPoint.X, rectangle.GetPoint2dAt(0).Y - 610, insertionPoint.Z),
                        Height = 180,
                        TextString = view
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
            if (isFront)
                blockName = "FrontBlock";
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
                for (int rowIndex = 0; rowIndex < rowsNum; rowIndex++)
                {
                    int rowCount = 0;
                    var panelsCount = rows[rowIndex].Panels.Count;
                    double num = 0;
                    double num2 = 0;
                    foreach (var panel in rows[rowIndex].Panels)
                    {
                        // Create a new block reference
                        if (panel.Height == 1220 && panel.Width == 1220)
                        {
                            Point3d currentInsertionPoint = new Point3d(insertionPoint.X, insertionPoint.Y + (rowIndex * 1220), insertionPoint.Z);
                            if (!isFront)
                            {
                                num += -609.9504;
                                num2 += 1056.5796;
                                currentInsertionPoint = new Point3d(currentInsertionPoint.X + num2, currentInsertionPoint.Y + num, currentInsertionPoint.Z);
                            }
                            else
                            {
                                num += 609.9504;
                                num2 += 1056.5796;
                                currentInsertionPoint = new Point3d(currentInsertionPoint.X + num2, (currentInsertionPoint.Y - 609.9504) + num, currentInsertionPoint.Z);
                            }


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
                    Height = 180,
                    TextString = view
                };
                //B Is Front
                if (isFront)
                {
                    text.Position = new Point3d(insertionPoint.X + (storageTank.Width / 1220 * 1056.5796) - text.GeometricExtents.MaxPoint.X / 2, insertionPoint.Y + storageTank.Length / 1220 * 609.9504 / 2, insertionPoint.Z);
                }
                else
                {
                    text.Position = new Point3d(insertionPoint.X + (storageTank.Width / 1220 * 1056.5796 / 2) - text.GeometricExtents.MaxPoint.X / 2, insertionPoint.Y - (((storageTank.Width / 2) / 1220) * 609.9504) - text.GeometricExtents.MaxPoint.Y - 400, insertionPoint.Z);
                }
                currentSpace.AppendEntity(text);
                tr.AddNewlyCreatedDBObject(text, true);

                // Commit the transaction to save the changes
                tr.Commit();

            }
        }

        private void BuildInnerView(Document doc, StorageTank storageTank, Point3d insertionPoint)
        {
            Point3d corner2 = new Point3d(storageTank.Length, storageTank.Width, 0);

            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {// Check if the block definition exists in the drawing
                BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                // Create a rectangle using a Polyline
                using (Polyline rectangle = new Polyline())
                {
                    rectangle.AddVertexAt(0, new Point2d(insertionPoint.X, insertionPoint.Y), 0, 0, 0);
                    rectangle.AddVertexAt(1, new Point2d(insertionPoint.X + corner2.X, insertionPoint.Y), 0, 0, 0);

                    rectangle.AddVertexAt(2, new Point2d(insertionPoint.X + corner2.X, insertionPoint.Y + corner2.Y), 0, 0, 0);
                    rectangle.AddVertexAt(3, new Point2d(insertionPoint.X, insertionPoint.Y + corner2.Y), 0, 0, 0);
                    rectangle.Closed = true;

                    // Add the rectangle to the current space
                    currentSpace.AppendEntity(rectangle);
                    tr.AddNewlyCreatedDBObject(rectangle, true);

                    DBText text = new DBText
                    {
                        Position = new Point3d(insertionPoint.X, rectangle.GetPoint2dAt(0).Y - 610, insertionPoint.Z),
                        Height = 180,
                        TextString = "Inner View"
                    };

                    currentSpace.AppendEntity(text);
                    tr.AddNewlyCreatedDBObject(text, true);
                }

                tr.Commit();
            }

        }

        private void BuildLeftAndRightInnerTriangleSupports(Document doc, StorageTank storageTank, Point3d insertionPoint)
        {
            string blockName = "Triangle";
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {// Check if the block definition exists in the drawing
                BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForWrite);


                for (int direction = 0; direction < 2; direction++)
                {
                    int rowCount = 1;
                    int offset = 300;
                    double rotationAngle = 0;
                    Point3d currentInsertionPoint;
                    switch (direction)
                    {
                        case 0:
                            currentInsertionPoint = new Point3d(insertionPoint.X, insertionPoint.Y + (rowCount * 1220) - 300, insertionPoint.Z);
                            rotationAngle = Math.PI;
                            break;
                        case 1:
                            currentInsertionPoint = new Point3d(insertionPoint.X + storageTank.Length - 324.323, insertionPoint.Y + (rowCount * 1220) - 300, insertionPoint.Z);

                            break;
                        default:
                            currentInsertionPoint = new Point3d(insertionPoint.X, insertionPoint.Y, insertionPoint.Z);
                            break;
                    }
                    // Loop to insert multiple instances of the block on top of each other
                    for (int panelIndex = 0; panelIndex < (storageTank.Width / 1220) - 1; panelIndex++)
                    {
                        if (panelIndex == (storageTank.Width / 1220) - 1)
                        {
                            for (int i = 0; i < 2; i++)
                            {

                                if (i == 0)
                                    offset = 300;
                                else
                                    offset = -300;
                                currentInsertionPoint = new Point3d(currentInsertionPoint.X, insertionPoint.Y + storageTank.Length + offset, insertionPoint.Z);
                                BlockReference blockRef = new BlockReference(currentInsertionPoint, bt[blockName]);

                                // Add the block reference to the current space
                                currentSpace.AppendEntity(blockRef);
                                tr.AddNewlyCreatedDBObject(blockRef, true);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                if (i == 0)
                                    offset = 100;
                                else
                                    offset = -300;
                                currentInsertionPoint = new Point3d(currentInsertionPoint.X, insertionPoint.Y + (rowCount * 1220) + offset, insertionPoint.Z);
                                BlockReference blockRef = new BlockReference(currentInsertionPoint, bt[blockName]);

                                Point3d center = blockRef.GeometricExtents.MinPoint + ((blockRef.GeometricExtents.MaxPoint - blockRef.GeometricExtents.MinPoint) / 2.0);

                                // Create a rotation matrix
                                Matrix3d rotationMatrix = Matrix3d.Rotation(rotationAngle, Vector3d.ZAxis, center);

                                blockRef.TransformBy(rotationMatrix);
                                // Add the block reference to the current space
                                currentSpace.AppendEntity(blockRef);
                                tr.AddNewlyCreatedDBObject(blockRef, true);
                            }
                        }

                        rowCount++;
                    }
                }
                // Commit the transaction to save the changes

                tr.Commit();

            }

        }

        private void BuildTopAndDownInnerTriangleSupports(Document doc, StorageTank storageTank, Point3d insertionPoint)
        {
            string blockName = "Triangle";
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForWrite);


                for (int direction = 0; direction < 2; direction++)
                {
                    int columnCount = 1;
                    double offset = 260.9386;
                    double rotationAngle = Math.PI;
                    Point3d currentInsertionPoint;
                    switch (direction)
                    {
                        case 0:
                            currentInsertionPoint = new Point3d(insertionPoint.X, insertionPoint.Y + 63.3844, insertionPoint.Z);
                            rotationAngle = -Math.PI / 2;
                            break;
                        case 1:
                            currentInsertionPoint = new Point3d(insertionPoint.X, insertionPoint.Y + storageTank.Width - offset, insertionPoint.Z);
                            rotationAngle = Math.PI / 2;
                            break;
                        default:
                            currentInsertionPoint = new Point3d(insertionPoint.X, insertionPoint.Y + 63.3844, insertionPoint.Z);
                            break;
                    }
                    for (int panelIndex = 0; panelIndex < (storageTank.Length / 1220) - 1; panelIndex++)
                    {
                        if (panelIndex == (storageTank.Length / 1220) - 1)
                        {
                            for (int i = 0; i < 2; i++)
                            {

                                if (i == 0)
                                    offset = 197.5543;
                                else
                                    offset = -197.5543;
                                currentInsertionPoint = new Point3d(insertionPoint.X + (columnCount * 1220) - offset, currentInsertionPoint.Y, insertionPoint.Z);
                                BlockReference blockRef = new BlockReference(currentInsertionPoint, bt[blockName]);

                                currentSpace.AppendEntity(blockRef);
                                tr.AddNewlyCreatedDBObject(blockRef, true);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                if (i == 0)
                                    offset = 98.7771;
                                else
                                    offset = -197.5543;
                                currentInsertionPoint = new Point3d(insertionPoint.X + (columnCount * 1220) + offset, currentInsertionPoint.Y, insertionPoint.Z);
                                BlockReference blockRef = new BlockReference(currentInsertionPoint, bt[blockName]);

                                Point3d center = blockRef.GeometricExtents.MinPoint + ((blockRef.GeometricExtents.MaxPoint - blockRef.GeometricExtents.MinPoint) / 2.0);
                                Matrix3d rotationMatrix = Matrix3d.Rotation(rotationAngle, Vector3d.ZAxis, center);

                                blockRef.TransformBy(rotationMatrix);
                                currentSpace.AppendEntity(blockRef);
                                tr.AddNewlyCreatedDBObject(blockRef, true);
                            }
                        }

                        columnCount++;
                    }
                }
                tr.Commit();

            }

        }

        private void BuildLeftAndRightInnerSupports(Document doc, StorageTank storageTank, Point3d insertionPoint)
        {
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {// Check if the block definition exists in the drawing
                BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForWrite);


                for (int direction = 0; direction < 2; direction++)
                {
                    int rowCount = 1;
                    double offset = 197.5543;
                    Point3d currentInsertionPoint = new Point3d(insertionPoint.X, insertionPoint.Y + (rowCount * 1220) - offset, insertionPoint.Z);
                    // Loop to insert multiple instances of the block on top of each other
                    for (int panelIndex = 0; panelIndex < (storageTank.Width / 1220) - 1; panelIndex++)
                    {
                        if (panelIndex == (storageTank.Width / 1220) - 1)
                        {
                            for (int i = 0; i < 2; i++)
                            {

                                if (i == 0)
                                    offset = 197.5543;
                                else
                                    offset = -202.1104;
                                currentInsertionPoint = new Point3d(currentInsertionPoint.X, insertionPoint.Y + storageTank.Length + offset + 25, insertionPoint.Z);


                                Polyline polyline = new Polyline();
                                Point3d corner2 = new Point3d(storageTank.Length, 50, 0);

                                polyline.AddVertexAt(0, new Point2d(currentInsertionPoint.X, currentInsertionPoint.Y), 0, 0, 0);
                                polyline.AddVertexAt(1, new Point2d(currentInsertionPoint.X + corner2.X, currentInsertionPoint.Y), 0, 0, 0);

                                polyline.AddVertexAt(2, new Point2d(currentInsertionPoint.X + corner2.X, currentInsertionPoint.Y + corner2.Y), 0, 0, 0);
                                polyline.AddVertexAt(3, new Point2d(currentInsertionPoint.X, currentInsertionPoint.Y + corner2.Y), 0, 0, 0);
                                polyline.Closed = true;


                                // Add the block reference to the current space
                                currentSpace.AppendEntity(polyline);
                                tr.AddNewlyCreatedDBObject(polyline, true);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                if (i == 0)
                                    offset = 197.5543;
                                else
                                    offset = -202.1104;
                                currentInsertionPoint = new Point3d(currentInsertionPoint.X, insertionPoint.Y + (rowCount * 1220) + offset - 25, insertionPoint.Z);
                                Polyline polyline = new Polyline();

                                Point3d corner2 = new Point3d(storageTank.Length - 100, 50, 0);

                                polyline.AddVertexAt(0, new Point2d(currentInsertionPoint.X + 100, currentInsertionPoint.Y), 0, 0, 0);
                                polyline.AddVertexAt(1, new Point2d(currentInsertionPoint.X + corner2.X, currentInsertionPoint.Y), 0, 0, 0);

                                polyline.AddVertexAt(2, new Point2d(currentInsertionPoint.X + corner2.X, currentInsertionPoint.Y + corner2.Y), 0, 0, 0);
                                polyline.AddVertexAt(3, new Point2d(currentInsertionPoint.X + 100, currentInsertionPoint.Y + corner2.Y), 0, 0, 0);
                                polyline.Closed = true;

                                // Add the block reference to the current space
                                currentSpace.AppendEntity(polyline);
                                tr.AddNewlyCreatedDBObject(polyline, true);
                            }
                        }

                        rowCount++;
                    }
                }
                // Commit the transaction to save the changes

                tr.Commit();

            }

        }

        private void BuildTopAndDownInnerSupports(Document doc, StorageTank storageTank, Point3d insertionPoint)
        {
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {// Check if the block definition exists in the drawing
                BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForWrite);


                for (int direction = 0; direction < 2; direction++)
                {
                    int rowCount = 1;
                    double offset = 197.5543;
                    Point3d currentInsertionPoint = new Point3d(insertionPoint.X + (rowCount * 1220) - offset, insertionPoint.Y, insertionPoint.Z);
                    // Loop to insert multiple instances of the block on top of each other
                    for (int panelIndex = 0; panelIndex < (storageTank.Length / 1220) - 1; panelIndex++)
                    {
                        if (panelIndex == (storageTank.Length / 1220) - 1)
                        {
                            for (int i = 0; i < 2; i++)
                            {

                                if (i == 0)
                                    offset = 197.5543;
                                else
                                    offset = -98.7771;
                                currentInsertionPoint = new Point3d(insertionPoint.X + storageTank.Width + offset + 12.5, currentInsertionPoint.Y, insertionPoint.Z);


                                Polyline polyline = new Polyline();
                                Point3d corner2 = new Point3d(50, storageTank.Width, 0);

                                polyline.AddVertexAt(0, new Point2d(currentInsertionPoint.X, currentInsertionPoint.Y), 0, 0, 0);
                                polyline.AddVertexAt(1, new Point2d(currentInsertionPoint.X + corner2.X, currentInsertionPoint.Y), 0, 0, 0);

                                polyline.AddVertexAt(2, new Point2d(currentInsertionPoint.X + corner2.X, currentInsertionPoint.Y + corner2.Y), 0, 0, 0);
                                polyline.AddVertexAt(3, new Point2d(currentInsertionPoint.X, currentInsertionPoint.Y + corner2.Y), 0, 0, 0);
                                polyline.Closed = true;


                                // Add the block reference to the current space
                                currentSpace.AppendEntity(polyline);
                                tr.AddNewlyCreatedDBObject(polyline, true);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                if (i == 0)
                                    offset = 197.5543;
                                else
                                    offset = -98.7771;
                                currentInsertionPoint = new Point3d(insertionPoint.X + (rowCount * 1220) + offset - 12.5, currentInsertionPoint.Y, insertionPoint.Z);
                                Polyline polyline = new Polyline();

                                Point3d corner2 = new Point3d(50, storageTank.Width - 200, 0);

                                polyline.AddVertexAt(0, new Point2d(currentInsertionPoint.X + 100, currentInsertionPoint.Y + 100), 0, 0, 0);
                                polyline.AddVertexAt(1, new Point2d(currentInsertionPoint.X + corner2.X, currentInsertionPoint.Y + 100), 0, 0, 0);

                                polyline.AddVertexAt(2, new Point2d(currentInsertionPoint.X + corner2.X, currentInsertionPoint.Y + corner2.Y + 100), 0, 0, 0);
                                polyline.AddVertexAt(3, new Point2d(currentInsertionPoint.X + 100, currentInsertionPoint.Y + corner2.Y + 100), 0, 0, 0);
                                polyline.Closed = true;

                                // Add the block reference to the current space
                                currentSpace.AppendEntity(polyline);
                                tr.AddNewlyCreatedDBObject(polyline, true);
                            }
                        }

                        rowCount++;
                    }
                }
                // Commit the transaction to save the changes

                tr.Commit();

            }

        }


        public void Initialize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            double newExtendValue = 120;

            byte red = 242;
            byte green = 113;
            byte blue = 114;

            Color newColor = Color.FromRgb(red, green, blue);
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {

                Application.SetSystemVariable("DIMEXO", newExtendValue);
                Application.SetSystemVariable("DIMTFAC", newExtendValue);
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
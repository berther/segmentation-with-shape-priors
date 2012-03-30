﻿using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Research.GraphBasedShapePrior.Tests
{
    [TestClass]
    public class ShapeEnergyTests
    {
        private static double TestShapeEnergyCalculationApproachesImpl(
            ShapeModel model, IEnumerable<Vector> vertices, IEnumerable<double> edgeWidths, int lengthGridSize, int angleGridSize, double eps)
        {
            // Create shape model and calculate energy in normal way
            Shape shape = new Shape(model, vertices, edgeWidths);
            double energy1 = shape.CalculateEnergy();

            // Calculate energy via generalized distance transforms
            ShapeConstraints constraints = ShapeConstraints.CreateFromConstraints(
                model, TestHelper.VerticesToConstraints(vertices), TestHelper.EdgeWidthsToConstraints(edgeWidths));
            BranchAndBoundSegmentationAlgorithm segmentator = new BranchAndBoundSegmentationAlgorithm();
            segmentator.ShapeModel = model;
            segmentator.LengthGridSize = lengthGridSize;
            segmentator.AngleGridSize = angleGridSize;
            double energy2 = segmentator.CalculateMinShapeEnergy(constraints);

            Assert.AreEqual(energy1, energy2, eps);

            return energy1;
        }

        private static void TestMeanShapeImpl(ShapeModel shapeModel, double eps)
        {
            Size imageSize = new Size(100, 160);
            Shape meanShape = shapeModel.FitMeanShape(imageSize);

            // Check GDT vs usual approach
            double energy = TestShapeEnergyCalculationApproachesImpl(
                shapeModel, meanShape.VertexPositions, meanShape.EdgeWidths, 1001, 1001, eps);

            // Check if energy is zero
            Assert.AreEqual(0, energy, eps);

            // Check if shape is inside given rect);
            foreach (Vector vertex in meanShape.VertexPositions)
                Assert.IsTrue(new RectangleF(0, 0, imageSize.Width, imageSize.Height).Contains((float)vertex.X, (float)vertex.Y));
        }

        private static void TestEdgeLimitsCommonImpl(
            VertexConstraints constraint1, VertexConstraints constraint2, out Range lengthRange, out Range angleRange)
        {
            ShapeConstraints constraintSet = ShapeConstraints.CreateFromConstraints(
                TestHelper.CreateTestShapeModelWith1Edge(), new[] { constraint1, constraint2 }, new[] { new EdgeConstraints(1, 10) });
            constraintSet.DetermineEdgeLimits(0, out lengthRange, out angleRange);

            GeneralizedDistanceTransform2D transform = new GeneralizedDistanceTransform2D(
                new Vector(0, -Math.PI * 2), new Vector(35, Math.PI * 2), new Size(2000, 2000));
            AllowedLengthAngleChecker allowedLengthAngleChecker = new AllowedLengthAngleChecker(constraint1, constraint2, transform, 1, 0);

            Random random = new Random(666);

            const int insideCheckCount = 1000;
            for (int i = 0; i < insideCheckCount; ++i)
            {
                Vector edgePoint1 =
                    constraint1.MinCoord +
                    new Vector(
                        random.NextDouble() * (constraint1.MaxCoord.X - constraint1.MinCoord.X),
                        random.NextDouble() * (constraint1.MaxCoord.Y - constraint1.MinCoord.Y));
                Vector edgePoint2 =
                    constraint2.MinCoord +
                    new Vector(
                        random.NextDouble() * (constraint2.MaxCoord.X - constraint2.MinCoord.X),
                        random.NextDouble() * (constraint2.MaxCoord.Y - constraint2.MinCoord.Y));

                Vector vec = edgePoint2 - edgePoint1;
                double length = vec.Length;
                double angle = Vector.AngleBetween(Vector.UnitX, vec);

                Assert.IsTrue(lengthRange.Contains(length));
                Assert.IsTrue(angleRange.Contains(angle));
                Assert.IsTrue(allowedLengthAngleChecker.IsAllowed(length, angle));
            }

            const int outsideCheckCount = 1000;
            for (int i = 0; i < outsideCheckCount; ++i)
            {
                Vector edgePoint1 =
                    constraint1.MinCoord +
                    new Vector(
                        (random.NextDouble() * 2 - 0.5) * (constraint1.MaxCoord.X - constraint1.MinCoord.X),
                        (random.NextDouble() * 2 - 0.5) * (constraint1.MaxCoord.Y - constraint1.MinCoord.Y));
                Vector edgePoint2 =
                    constraint2.MinCoord +
                    new Vector(
                        (random.NextDouble() * 2 - 0.5) * (constraint2.MaxCoord.X - constraint2.MinCoord.X),
                        (random.NextDouble() * 2 - 0.5) * (constraint2.MaxCoord.Y - constraint2.MinCoord.Y));

                Vector vec = edgePoint2 - edgePoint1;
                double length = vec.Length;
                double angle = Vector.AngleBetween(Vector.UnitX, vec);

                // We've generated too large edge
                if (length > transform.GridMax.X)
                    continue;

                bool definitelyOutside = !lengthRange.Contains(length) || !angleRange.Contains(angle);
                bool outside = !allowedLengthAngleChecker.IsAllowed(length, angle);
                Assert.IsTrue(!definitelyOutside || outside);
            }
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches1()
        {
            List<Vector> vertices = new List<Vector> { new Vector(0, 0), new Vector(80, 0), new Vector(80, 100) };
            List<double> edgeWidths = new List<double> { 10, 15 };

            TestShapeEnergyCalculationApproachesImpl(
                TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), vertices, edgeWidths, 2001, 2001, 0.1);
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches2()
        {
            List<Vector> vertices = new List<Vector> { new Vector(0, 0), new Vector(40, 0), new Vector(0, 42) };
            List<double> edgeWidths = new List<double> { 10, 15 };

            TestShapeEnergyCalculationApproachesImpl(
                TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), vertices, edgeWidths, 2001, 2001, 0.2);
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches3()
        {
            List<Vector> vertices = new List<Vector>
            {
                new Vector(0, 0),
                new Vector(40, 0),
                new Vector(40, 50),
                new Vector(80, 70),
                new Vector(30, 55),
                new Vector(10, -50)
            };
            List<double> edgeWidths = new List<double> { 10, 11, 12, 13, 14 };

            TestShapeEnergyCalculationApproachesImpl(TestHelper.CreateTestShapeModel5Edges(), vertices, edgeWidths, 2001, 2001, 2);
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches4()
        {
            List<Vector> vertices = new List<Vector>
            {
                new Vector(0, 0),
                new Vector(40, 0),
                new Vector(3, -40),
                new Vector(37, -43),
                new Vector(2, -90),
                new Vector(-35, -95)
            };
            List<double> edgeWidths = new List<double> { 10, 11, 12, 13, 14 };

            TestShapeEnergyCalculationApproachesImpl(TestHelper.CreateTestShapeModel5Edges(), vertices, edgeWidths, 3001, 3001, 2);
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches5()
        {
            List<Vector> vertices = new List<Vector>
            {
                new Vector(0, 0),
                new Vector(-40, -1),
                new Vector(3, -40),
                new Vector(37, -43),
                new Vector(2, -90),
                new Vector(-35, -95)
            };
            List<double> edgeWidths = new List<double> { 10, 11, 12, 13, 14 };

            TestShapeEnergyCalculationApproachesImpl(TestHelper.CreateLetterShapeModel(), vertices, edgeWidths, 3001, 3001, 2);
        }

        [TestMethod]
        public void TestMeanShape1()
        {
            TestMeanShapeImpl(TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), 1e-4);
        }

        [TestMethod]
        public void TestMeanShape2()
        {
            TestMeanShapeImpl(TestHelper.CreateTestShapeModel5Edges(), 1e-4);
        }

        [TestMethod]
        public void TestMeanShape3()
        {
            TestMeanShapeImpl(TestHelper.CreateLetterShapeModel(), 1e-4);
        }

        [TestMethod]
        public void TestShapeTwist()
        {
            const double edgeLength = 100;
            const double startAngle = Math.PI * 0.5;

            ShapeModel shapeModel = TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1);
            List<Vector> vertices = new List<Vector>();
            vertices.Add(new Vector(0, 0));
            vertices.Add(new Vector(Math.Cos(startAngle) * edgeLength, Math.Sin(startAngle) * edgeLength));
            vertices.Add(new Vector());

            List<double> edgeWidths = new List<double> { 10, 10 };

            const int iterationCount = 10;
            const double angleStep = 2 * Math.PI / iterationCount;
            Shape lastShape = null;
            for (int i = 0; i < iterationCount; ++i)
            {
                double angle = startAngle + Math.PI * 0.5 + angleStep * i;
                vertices[2] = new Vector(vertices[1].X + edgeLength * Math.Cos(angle), vertices[1].Y + edgeLength * Math.Sin(angle));
                Shape shape = new Shape(shapeModel, vertices, edgeWidths);

                // Test if energy is increasing/decreasing properly
                if (i <= iterationCount / 2)
                    Assert.IsTrue(lastShape == null || lastShape.CalculateEnergy() < shape.CalculateEnergy());
                else
                    Assert.IsTrue(lastShape.CalculateEnergy() > shape.CalculateEnergy());

                TestShapeEnergyCalculationApproachesImpl(shapeModel, vertices, edgeWidths, 2001, 2001, 1e-6);

                lastShape = shape;
            }
        }

        [TestMethod]
        public void TestEdgeLimits1()
        {
            VertexConstraints constraint1 = new VertexConstraints(new Vector(-10, -10), new Vector(10, 10));
            VertexConstraints constraint2 = new VertexConstraints(new Vector(11, -7), new Vector(13, 15));

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);

            Assert.IsFalse(angleRange.Outside);
            Assert.IsTrue(angleRange.Contains(0));
            Assert.IsFalse(angleRange.Contains(Math.PI));
            Assert.IsFalse(angleRange.Contains(Math.PI * 0.5));
            Assert.IsFalse(angleRange.Contains(-Math.PI * 0.5));
            Assert.IsFalse(angleRange.Contains(-Math.PI));
        }

        [TestMethod]
        public void TestEdgeLimits2()
        {
            VertexConstraints constraint1 = new VertexConstraints(new Vector(11, -7), new Vector(13, 15));
            VertexConstraints constraint2 = new VertexConstraints(new Vector(-10, -10), new Vector(10, 10));

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);

            Assert.IsTrue(angleRange.Outside);
            Assert.IsFalse(angleRange.Contains(0));
            Assert.IsTrue(angleRange.Contains(Math.PI));
            Assert.IsTrue(angleRange.Contains(-Math.PI));
            Assert.IsFalse(angleRange.Contains(Math.PI * 0.5));
            Assert.IsFalse(angleRange.Contains(-Math.PI * 0.5));
        }

        [TestMethod]
        public void TestEdgeLimits3()
        {
            VertexConstraints constraint1 = new VertexConstraints(new Vector(0, 0), new Vector(10, 10));
            VertexConstraints constraint2 = new VertexConstraints(new Vector(11, 11), new Vector(12, 16));

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);

            Assert.IsFalse(angleRange.Outside);
            Assert.IsTrue(angleRange.Contains(Math.PI * 0.25));
            Assert.IsFalse(angleRange.Contains(0));
            Assert.IsFalse(angleRange.Contains(Math.PI * 0.5));

            Assert.IsFalse(lengthRange.Contains(0));
            Assert.IsFalse(lengthRange.Contains(1));
        }

        [TestMethod]
        public void TestEdgeLimits4()
        {
            const double eps = 0.01;
            VertexConstraints constraint1 = new VertexConstraints(new Vector(0, 0), new Vector(1 - eps, 1 - eps));
            VertexConstraints constraint2 = new VertexConstraints(new Vector(1 + eps, 1 + eps), new Vector(2, 2));

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);

            Assert.IsFalse(angleRange.Outside);
            Assert.IsTrue(angleRange.Contains(0.01));
            Assert.IsFalse(angleRange.Contains(-Math.PI * 0.501));
            Assert.IsFalse(angleRange.Contains(-Math.PI));
            Assert.IsFalse(angleRange.Contains(Math.PI * 0.501));
            Assert.IsFalse(angleRange.Contains(Math.PI));

            Assert.IsFalse(lengthRange.Contains(3));
            Assert.IsTrue(lengthRange.Contains(0.05));
            Assert.IsTrue(lengthRange.Contains(2.8));
        }

        [TestMethod]
        public void TestEdgeLimits5()
        {
            VertexConstraints constraint1 = new VertexConstraints(new Vector(-10, 8), new Vector(10, 10));
            VertexConstraints constraint2 = new VertexConstraints(new Vector(5, 0), new Vector(6, 7));

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);
        }

        [TestMethod]
        public void TestEdgeLimits6()
        {
            VertexConstraints constraint1 = new VertexConstraints(new Vector(0, 0), new Vector(5, 5));
            VertexConstraints constraint2 = new VertexConstraints(new Vector(4, 4), new Vector(10, 9));

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);
        }

        [TestMethod]
        public void TestEdgeLimits7()
        {
            VertexConstraints constraint1 = new VertexConstraints(new Vector(0, 0), new Vector(10, 10));
            VertexConstraints constraint2 = new VertexConstraints(new Vector(5, 5), new Vector(8, 8));

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);
        }

        [TestMethod]
        public void TestVertexConstraintSplitsNonIntersection()
        {
            VertexConstraints constraints = new VertexConstraints(new Vector(0, 0), new Vector(1, 1));

            List<VertexConstraints> split = constraints.Split();
            Assert.IsTrue(split.Count == 4);
            for (int i = 0; i < split.Count; ++i)
            {
                for (int j = i + 1; j < split.Count; ++j)
                {
                    Range xRange1 = new Range(split[i].MinCoord.X, split[i].MaxCoord.X, false);
                    Range yRange1 = new Range(split[i].MinCoord.Y, split[i].MaxCoord.Y, false);
                    Range xRange2 = new Range(split[j].MinCoord.X, split[j].MaxCoord.X, false);
                    Range yRange2 = new Range(split[j].MinCoord.Y, split[j].MaxCoord.Y, false);

                    Assert.IsFalse(xRange1.IntersectsWith(xRange2) && yRange1.IntersectsWith(yRange2));
                }
            }
        }

        [TestMethod]
        public void TestEdgeConstraintSplitsNonIntersection()
        {
            EdgeConstraints constraints = new EdgeConstraints(3, 5);
            List<EdgeConstraints> split = constraints.Split();
            Assert.IsTrue(split.Count == 2);

            Range range1 = new Range(split[0].MinWidth, split[0].MaxWidth, false);
            Range range2 = new Range(split[1].MinWidth, split[1].MaxWidth, false);
            Assert.IsFalse(range1.IntersectsWith(range2));
        }
    }
}

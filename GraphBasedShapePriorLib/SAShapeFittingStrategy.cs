﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior
{
    public class SAShapeFittingStrategy : IShapeFittingStrategy
    {
        public List<Shape> FitShapes(ShapeModel shapeModel, Image2D<bool> mask)
        {
            return new List<Shape>();
        }
    }
}
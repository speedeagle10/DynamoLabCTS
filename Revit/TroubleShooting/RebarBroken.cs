using Autodesk.DesignScript.Runtime;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Dynamo.Graph.Nodes;
using Revit.Elements;
using Revit.GeometryConversion;
using RevitServices.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DynamoLab.Revit.TroubleShooting
{
    /// <summary>
    /// Class for troubleshooting
    /// </summary>
    public class RebarBroken
    {
        private RebarBroken() { }


        #region "split rebar"


        /// <summary>
        /// Break rebar at parameter for rebar coupler placement
        /// </summary>
        /// <param name="dynamoRebar"> rebar select at the Revit model.</param>
        /// <param name="splitPara"> parameter at which to split curve.</param>
        /// <returns name="Rebar">new created Rebar</returns> 
        [NodeCategory("Create")]  //3 option Create / Actions / Query
        public static List<global::Revit.Elements.Element> Split1StraightRebarByParameter(global::Revit.Elements.Element dynamoRebar, double splitPara)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Rebar revitRebar = (Rebar)dynamoRebar.InternalElement;

            //https://www.revitapidocs.com/2023/6edc946f-d8a3-ee78-adbb-7d5359501ed3.htm
            RebarShape rebarShape = (RebarShape)dynamoDocument.GetElement(revitRebar.GetShapeId());

            //https://www.revitapidocs.com/2023/ec72e836-ea5a-8d74-40f0-55344ae095bd.htm
            RebarStyle rebarStyle = rebarShape.RebarStyle;

            //get RebarBarType from selected Rebar
            //https://www.revitapidocs.com/2023/cc66ca8e-302e-f072-edca-d847bcf14c86.htm
            RebarBarType rebarBarType = (RebarBarType)dynamoDocument.GetElement(revitRebar.GetTypeId());

            //https://www.revitapidocs.com/2023/016d53d9-0ef5-99d1-b12f-089f04df3952.htm
            //0 for the start hook, 1 for the end hook.
            RebarHookType startRebarHookType = (RebarHookType)dynamoDocument.GetElement(revitRebar.GetHookTypeId(0));
            RebarHookType endRebarHookType = (RebarHookType)dynamoDocument.GetElement(revitRebar.GetHookTypeId(1));

            //https://www.revitapidocs.com/2023/0aabc992-1723-9f78-aff7-ef9760f8a64b.htm
            //0 for the start hook, 1 for the end hook.
            RebarHookOrientation startRebarHookOrientation = (RebarHookOrientation)revitRebar.GetHookOrientation(0);
            RebarHookOrientation endRebarHookOrientation = (RebarHookOrientation)revitRebar.GetHookOrientation(1);

            //get rebar host elment, for example structural column or structural beam
            ElementId host = revitRebar.GetHostId();
            Autodesk.Revit.DB.Element rebarHost = dynamoDocument.GetElement(host);

            //https://www.revitapidocs.com/2023/70fd7426-f4a4-591c-8c06-3c18dda45e7d.htm
            List<Autodesk.Revit.DB.Curve> centerCurves = revitRebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0).ToList();

            //if (centerCurves.Count =1)  {  }
            Autodesk.Revit.DB.Curve centerCurve = centerCurves[0];
            List<Autodesk.Revit.DB.Curve> newCurveList = SplitCurveByParameterList(centerCurve, splitPara);

            //// get the center line curves of the rebar elements
            //foreach (Autodesk.Revit.DB.Curve curve in curves)
            //{
            //    // Get a DesignScript Curve from the Revit curve
            //    Autodesk.DesignScript.Geometry.Curve geocurve = curve.ToProtoType();
            //    //curveList.Add(geocurve);
            //}

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            List<global::Revit.Elements.Element> result = new List<global::Revit.Elements.Element>();

            //https://www.revitapidocs.com/2023/cf39a5b8-d7f3-d073-0120-358dd3afab21.htm
            //Returns true if the rebar is free form and false if shape driven.
            if (revitRebar.IsRebarFreeForm())// only need free form bars
            {
                foreach (Autodesk.Revit.DB.Curve newCurve in newCurveList)
                {
                    IList<Autodesk.Revit.DB.Curve> curves = new List<Autodesk.Revit.DB.Curve>();
                    curves.Add(newCurve);

                    XYZ normal = GetStraightLinePlaneNormal(newCurve);

                    //https://www.revitapidocs.com/2023/069ab1d6-e41d-de8e-cc56-8f4d6e776926.htm
                    Rebar rebar = Rebar.CreateFromCurves(dynamoDocument, rebarStyle, rebarBarType, startRebarHookType, endRebarHookType, rebarHost, normal, curves, startRebarHookOrientation, endRebarHookOrientation, true, true);
                    result.Add(rebar.ToDSType(true));
                }
            }
            else //shapen driven rebar
            {
                foreach (Autodesk.Revit.DB.Curve newCurve in newCurveList)
                {
                    IList<Autodesk.Revit.DB.Curve> curves = new List<Autodesk.Revit.DB.Curve>();
                    curves.Add(newCurve);

                    XYZ normal = GetStraightLinePlaneNormal(newCurve);

                    //https://www.revitapidocs.com/2023/10ddc28e-a410-5f29-6fe9-d4b73f917c54.htm
                    Rebar rebar = Rebar.CreateFromCurvesAndShape(dynamoDocument, rebarShape, rebarBarType, startRebarHookType, endRebarHookType, rebarHost, normal, curves, startRebarHookOrientation, endRebarHookOrientation);
                    result.Add(rebar.ToDSType(true));
                }
            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return result;
        }


        /// <summary>
        /// Break rebar set at parameter for rebar coupler placement
        /// </summary>
        /// <param name="dynamoRebarSet"> rebar set select at the Revit model.</param>
        /// <returns name="LayoutRule"> Identifies the layout rule of rebar set.Could be Single, Fix Number, Maximum spacing, number with spacing, minimum clear spacing </returns> 
        /// <returns name="DistributionType"> The type of rebar distribution(also known as Rebar Set Type).The possible values of this property are:Uniform and VaryingLength </returns> 
        /// <returns name="Number of RebarPosition"> The number of positions is equal to the number of actual bars (the Quantity), plus the number of bars that are excluded. </returns> 
        /// <returns name="RebarQuantity"> Identifies the number of bars in rebar set. Quantity is equal to NumberOfBarPositions if all the bars are included. 
        /// If any bars are excluded, they are not counted in the Quantity.</returns> 
        /// <returns name="ArrayLength"> Identifies the distribute length of rebar set, works for Fix Number, Maximum spacing,minimum clear spacing. </returns> 
        /// <returns name="RebarSetSpacing"> spacing parameter of rebar set, could be spacing for  </returns> 
        /// <returns name="RebarSetMaxSpacing"> Identifies the maximum spacing between rebar in rebar set. </returns> 
        /// <returns name="IncludeFirstBar"> Identifies if the first bar in rebar set is shown. </returns> 
        /// <returns name="IncludeLastBar"> Identifies if the last bar in rebar set is shown. </returns> 
        /// <returns name="DynamoNormal"> A unit-length vector normal to the plane of the rebar. Note: for single rebar, use normal from GetRebarProperties_ZTN </returns> 
        /// <returns name="RebarsOnNormalSide"> Identifies if the bars of the rebar set are on the same side of the rebar plane indicated by the normal.
        /// True if the bars of rebar set are on the same side of the rebar plane indicated by the normal, and false if the bars are on the opposite side. </returns>
        /// <returns name="FirstCurves"> get the curve behind the first rebar shape of the rebar set </returns> 
        [NodeCategory("Actions")]  //3 option Create / Actions / Query
        [MultiReturn(new[] { "LayoutRule", "DistributionType", "NumberOfBarPositions", "RebarQuantity",  "ArrayLength","RebarSetSpacing", "RebarSetMaxSpacing", 
            "IncludeFirstBar", "IncludeLastBar","DynamoNormal","RebarsOnNormalSide","FirstCurves" })]
        public static Dictionary<string, object> GetRebarSetInformation(global::Revit.Elements.Element dynamoRebarSet)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Rebar revitRebarSet = (Rebar)dynamoRebarSet.InternalElement;

            //https://www.revitapidocs.com/2016/69eb7a49-7e5a-da45-6579-c91386888a7f.htm
            // And the layout rule, result could be Single, Fix Number, Maximum spacing, number with spacing, minimum clear spacing
            RebarLayoutRule layoutRule = revitRebarSet.LayoutRule;
            //string layoutRuleName = layoutRule.ToString();

            //https://www.revitapidocs.com/2023/d5518e91-ce1f-7a3f-01bf-e3e3727ed42d.htm
            //The possible values of this property are:Uniform and VaryingLength
            //For a uniform distribution type: all bars parameters are the same as the first bar in set.For a varying length distribution type:
            //bars parameters can vary(primarly in length) taking in consideration the constraints of the first bar in set.
            DistributionType distributionType = revitRebarSet.DistributionType;

            // Now you can get the number of bar positions
            int numberOfBarPositions = revitRebarSet.NumberOfBarPositions;

            //Quantity Property
            //https://www.revitapidocs.com/2023/6d042353-dea0-e851-bed7-1143559e03db.htm
            //Quantity is equal to NumberOfBarPositions if IncludeFirstBar and IncludeLastBar are set. If any end bars are excluded, they are not counted in the Quantity.
            // Now you can get the number of bar positions
            int rebarQuantity = revitRebarSet.Quantity;

            // Get the parameter you need: REBAR_ELEM_BAR_SPACING => Spacing
            // used with Maximum spacing, number with spacing, minimum clear spacing
            Autodesk.Revit.DB.Parameter rebarSetSpacingParam = revitRebarSet.get_Parameter(BuiltInParameter.REBAR_ELEM_BAR_SPACING);
            double rebarSetSpacingInternal = rebarSetSpacingParam.AsDouble();
            //Parameter rebarSetSpacing = revitRebarSet.LookupParameter("Spacing"); 

            Autodesk.Revit.DB.Units getDocUnits = dynamoDocument.GetUnits();
            //https://www.revitapidocs.com/2023/32e858f2-d143-fe2c-76a5-38485382fb95.htm
            ForgeTypeId getDisplayUnits = getDocUnits.GetFormatOptions(Autodesk.Revit.DB.SpecTypeId.Length).GetUnitTypeId();

            //https://www.revitapidocs.com/2023/e70d4936-ecf2-dfbf-8caf-aac5d6a78d36.htm
            //Converts a value from Revit's internal units to a given unit.
            //Converts to a given value from Revit's internal value by changing the Units
            //Tip: The Revit API uses decimal feet as its internal units system. This cannot be changed,so if you prefer working
            //in other unit systems(i.e.metric) or if your inputs are not decimal feet, you will have to perform the units
            //conversion within your code. It is also worth mentioning for angles, it uses radians, not degrees.
            double rebarSetSpacing = Autodesk.Revit.DB.UnitUtils.ConvertFromInternalUnits(rebarSetSpacingInternal, getDisplayUnits);

            double rebarSetMaxSpacing =0;
            bool includeFirstBar = true;
            bool includeLastBar = true;
            if (layoutRule != RebarLayoutRule.Single)
            {
                //used with Maximum spacing, number with spacing, minimum clear spacing, same as the value from Spacing parameter.
                //https://www.revitapidocs.com/2023/1e7105e5-8d08-26ed-d97d-76754753fded.htm
                double rebarSetMaxSpacingInternal = revitRebarSet.MaxSpacing;
                rebarSetMaxSpacing = Autodesk.Revit.DB.UnitUtils.ConvertFromInternalUnits(rebarSetMaxSpacingInternal, getDisplayUnits);

                //https://www.revitapidocs.com/2023/099e72c3-a92f-b14c-fada-ef91241f5152.htm
                //Identifies if the first bar in rebar set is shown.
                includeFirstBar = revitRebarSet.IncludeFirstBar;
                //https://www.revitapidocs.com/2023/5b3663d8-9b7f-12de-d20a-895259bea9ac.htm
                includeLastBar = revitRebarSet.IncludeLastBar;
            }

            //https://www.revitapidocs.com/2023/c77085bd-db18-4869-bb2a-1e5c702e273a.htm
            //Returns an interface providing access to shape-driven properties and methods for this Rebar element.
            RebarShapeDrivenAccessor rebarShapeDrivenAccessor = revitRebarSet.GetShapeDrivenAccessor();
            //Identifies the distribution path length of rebar set.works for Fix Number, Maximum spacing,minimum clear spacing
            double arrayLength = 0;
            if (layoutRule != RebarLayoutRule.Single && layoutRule != RebarLayoutRule.NumberWithSpacing) 
            {
                //used with Maximum spacing, number with spacing, minimum clear spacing, same as the value from Spacing parameter.
                //https://www.revitapidocs.com/2023/b9d15e52-d912-a5ad-9fb8-4ff22849ec1f.htm
                double arrayLengthInternal = rebarShapeDrivenAccessor.ArrayLength;
                arrayLength = Autodesk.Revit.DB.UnitUtils.ConvertFromInternalUnits(arrayLengthInternal, getDisplayUnits);

            }

            //https://www.revitapidocs.com/2023/dc6806bd-c813-1964-b768-2177e718bb5f.htm
            //Identifies if the bars of the rebar set are on the same side of the rebar plane indicated by the normal.
            //True if the bars of rebar set are on the same side of the rebar plane indicated by the normal, and false if the bars are on the opposite side.
            bool rebarsOnNormalSide = rebarShapeDrivenAccessor.BarsOnNormalSide;

            //https://www.revitapidocs.com/2023/d6ecbc19-e4dd-536c-b83a-1019b1663e04.htm
            //A unit-length vector normal to the plane of the rebar
            XYZ revitNormal = rebarShapeDrivenAccessor.Normal;
            Autodesk.DesignScript.Geometry.Vector dynamoNormal = revitNormal.ToVector();

            //https://www.revitapidocs.com/2023/70fd7426-f4a4-591c-8c06-3c18dda45e7d.htm
            //https://www.revitapidocs.com/2023/7be7e413-bfac-bbd3-17b6-ff2008771afa.htm
            //A chain of curves representing the centerline of the rebar. For rebarset, it will only give the center curve of the 1st rebar.
            List<Autodesk.Revit.DB.Curve> centerCurves = revitRebarSet.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0).ToList();

            //List<Autodesk.Revit.DB.Curve> curves = new List<Autodesk.Revit.DB.Curve>();
            List<Autodesk.DesignScript.Geometry.Curve> curveList = new List<Autodesk.DesignScript.Geometry.Curve>();
            foreach (Autodesk.Revit.DB.Curve curve in centerCurves)
            {
                // Get a DesignScript Curve from the Revit curve
                Autodesk.DesignScript.Geometry.Curve geocurve = curve.ToProtoType();
                curveList.Add(geocurve);
            }

            return new Dictionary<string, object>
            {
                { "LayoutRule", layoutRule },
                { "DistributionType",distributionType },

                { "NumberOfBarPositions", numberOfBarPositions },
                { "RebarQuantity", rebarQuantity },

                { "ArrayLength", arrayLength},

                { "RebarSetSpacing", rebarSetSpacing },
                { "RebarSetMaxSpacing", rebarSetMaxSpacing },

                { "IncludeFirstBar", includeFirstBar},
                { "IncludeLastBar", includeLastBar},

                { "DynamoNormal", dynamoNormal},
                { "RebarsOnNormalSide", rebarsOnNormalSide},

                { "FirstCurves", curveList},
            };

        }


        /// <summary>
        /// Index of a major segment of the rebar
        /// </summary>
        /// <param name="rebarShape"> RebarShape specifies the shape type for a Rebar instance..</param>
        /// <returns name="RebarMajorSegmentIndex"> Index of a segment that can be considered the most important. Revit attempts to preserve 
        /// the orientation of this segment when a Rebar instance changes its RebarShape to one with a different number of segments. </returns> 
        [NodeCategory("Actions")]  //3 option Create / Actions / Query
        public static int GetMajorSetmentId(RebarShape rebarShape)
        {
            // Get the shape definition
            //https://www.revitapidocs.com/2023/80019bb8-76a4-f6e1-476d-2f9992286adb.htm
            //RebarShapeDefinitionBySegments shapeDefinition = rebarShape.GetRebarShapeDefinition() as RebarShapeDefinitionBySegments;
            RebarShapeDefinition rebarShapeDefinition = rebarShape.GetRebarShapeDefinition();
            RebarShapeDefinitionBySegments shapeDefinition = (RebarShapeDefinitionBySegments)rebarShapeDefinition;


            int majorSegmentIndex = shapeDefinition.MajorSegmentIndex;
            //int majorSegmentIndex=-1;
            //if (shapeDefinition != null)
            //{
            //    // Get the major segment index
            //    //https://www.revitapidocs.com/2023/fd0d677f-aad6-cb00-1b8f-9bd05a6f3dc6.htm
            //    majorSegmentIndex = shapeDefinition.MajorSegmentIndex;

            //    // Now you can use majorSegmentIndex to get the major segment
            //    //https://www.revitapidocs.com/2023/7c31cede-dc2f-c8fd-5cd7-adbd610fef14.htm
            //    //Return a reference to one of the segments in the definition.
            //    RebarShapeSegment majorSegment = shapeDefinition.GetSegment(majorSegmentIndex);
            //}
            return majorSegmentIndex;
        }


        /// <summary>
        /// The normal to the plane that the rebar curves lie on, Assuming 'curve' is your Curve object and it's not a straight line
        /// </summary>
        /// <param name="curves"> Curve from Rebar Line.</param>
        /// <returns name="Normal Vector"> The normal to the plane that the rebar curves lie on </returns> 
        [NodeCategory("Actions")]  //3 option Create / Actions / Query
        public static XYZ GetRebarCurvePlaneNormal(List<Autodesk.Revit.DB.Curve> curves)
        {
            if (curves.Count == 1 && curves[0] is Autodesk.Revit.DB.Line)
            {
                //throw new InvalidOperationException("Not enough curves to form a plane.");
                return GetStraightLinePlaneNormal(curves[0]);
            }
            else
            {
                // Get three non-collinear points
                Autodesk.Revit.DB.XYZ point1 = curves[0].GetEndPoint(0);
                Autodesk.Revit.DB.XYZ point2 = curves[0].GetEndPoint(1);
                Autodesk.Revit.DB.XYZ point3 = curves[curves.Count - 1].GetEndPoint(0);

                // Create a plane
                Autodesk.Revit.DB.Plane plane = Autodesk.Revit.DB.Plane.CreateByThreePoints(point1, point2, point3);

                // Return the normal to the plane
                return plane.Normal;
            }
        }

        /// <summary>
        /// The normal to the plane that the rebar curves lie on, Assuming 'curve' is your Curve object and it's a straight line
        /// </summary>
        /// <param name="curve"> Curve from Rebar line.</param>
        /// <returns name="Normal Vector"> The normal to the plane that the rebar curves lie on </returns> 
        [NodeCategory("Actions")]  //3 option Create / Actions / Query
        protected static XYZ GetStraightLinePlaneNormal(Autodesk.Revit.DB.Curve curve)
        {
            // Check if curve is null
            if (curve == null)
            {
                throw new ArgumentNullException("curve");
            }

            // Get the direction of the curve
            XYZ curveDirection = curve.GetEndPoint(1) - curve.GetEndPoint(0);

            // Create an arbitrary vector not parallel to the curve
            XYZ arbitraryVector = new XYZ(1, 0, 0);
            if (curveDirection.IsAlmostEqualTo(arbitraryVector))
            {
                arbitraryVector = new XYZ(0, 1, 0);
            }
            // Create a plane normal by taking the cross product of the curve direction and the arbitrary vector
            XYZ planeNormal = curveDirection.CrossProduct(arbitraryVector);

            return planeNormal;
        }

        /// <summary>
        /// The normal to the plane that the rebar curves lie on, Assuming 'curve' is your Curve object and it's not a straight line
        /// </summary>
        /// <param name="rebar"> Rebar generated in Revit.</param>
        /// <returns name="Normal Vector"> The normal to the plane that the rebar curves lie on </returns> 
        [NodeCategory("Actions")]  //3 option Create / Actions / Query
        public static XYZ GetRebarPlaneNormal(Rebar rebar)
        {
            var curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
            if (curves.Count == 1 && (curves[0] is Autodesk.Revit.DB.Line))
            {
                //throw new InvalidOperationException("Not enough curves to form a plane.");
                return GetStraightLinePlaneNormal(curves[0]);
            }
            else
            {
                // Get three non-collinear points
                XYZ point1 = curves[0].GetEndPoint(0);
                XYZ point2 = curves[0].GetEndPoint(1);
                XYZ point3 = curves[curves.Count - 1].GetEndPoint(0);

                // Create a plane
                Autodesk.Revit.DB.Plane plane = Autodesk.Revit.DB.Plane.CreateByThreePoints(point1, point2, point3);

                // Return the normal to the plane
                return plane.Normal;
            }
        }




        /// <summary>
        /// Split curve at parameter
        /// </summary>
        /// <param name="revitCurve"> Curve from Revit.</param>
        /// <param name="splitParam"> split parameter for the curve.</param>
        /// <returns name="Curves">new created curves</returns> 
        protected static Autodesk.Revit.DB.Curve[] SplitCurveByParameterArray(Autodesk.Revit.DB.Curve revitCurve, double splitParam)
        {
            // Check if the parameter is within the curve's range
            if (splitParam <= revitCurve.GetEndParameter(0) || splitParam >= revitCurve.GetEndParameter(1))
            {
                throw new Exception("The split parameter is outside the curve's range.");
            }

            // Split the curve
            Autodesk.Revit.DB.Curve[] splitCurves = new Autodesk.Revit.DB.Curve[2];
            //https://www.revitapidocs.com/2017/071f6ca9-f243-4655-924c-7beb881b100f.htm
            //Returns a copy of this curve.
            splitCurves[0] = revitCurve.Clone();
            //https://www.revitapidocs.com/2017/f9bc51b4-50a3-de79-4d7e-401ab2dbebb2.htm
            //Changes the bounds of this curve to the specified values.
            splitCurves[0].MakeBound(revitCurve.GetEndParameter(0), splitParam* revitCurve.GetEndParameter(1));

            splitCurves[1] = revitCurve.Clone();
            //https://www.revitapidocs.com/2017/0f4b2c25-35f8-4e3c-c71a-0d41fb6935ce.htm
            //Returns the raw parameter value at the start or end of this curve.
            splitCurves[1].MakeBound(splitParam * revitCurve.GetEndParameter(1), revitCurve.GetEndParameter(1));

            return splitCurves;
        }

        /// <summary>
        /// Split curve at parameter
        /// </summary>
        /// <param name="revitCurve"> Curve from Revit.</param>
        /// <param name="splitParam"> split parameter for the curve.</param>
        /// <returns name="Curves">new created curves</returns> 
        protected static List<Autodesk.Revit.DB.Curve> SplitCurveByParameterList(Autodesk.Revit.DB.Curve revitCurve, double splitParam)
        {
            // Check if the parameter is within the curve's range
            if (splitParam <= revitCurve.GetEndParameter(0) || splitParam >= revitCurve.GetEndParameter(1))
            {
                throw new Exception("The split parameter is outside the curve's range.");
            }

            // Split the curve
            List<Autodesk.Revit.DB.Curve> splitCurves = new List<Autodesk.Revit.DB.Curve>();
            Autodesk.Revit.DB.Curve firstCurve = revitCurve.Clone();
            firstCurve.MakeBound(revitCurve.GetEndParameter(0), splitParam * revitCurve.GetEndParameter(1));
            splitCurves.Add(firstCurve);

            Autodesk.Revit.DB.Curve secondCurve = revitCurve.Clone();
            secondCurve.MakeBound(splitParam * revitCurve.GetEndParameter(1), revitCurve.GetEndParameter(1));
            splitCurves.Add(secondCurve);

            return splitCurves;
        }

        //https://github.com/DynamoDS/designscript-archive/blob/2cfafd1299af0ffddacf77c41f51150556e88b80/Libraries/ProtoGeometry/Geometry/Curve.cs#L4
        /// <summary>
        /// Split curve at parameter
        /// </summary>
        /// <param name="dynamoCurve"> Curve from Dynamo.</param>
        /// <param name="splitParam"> split parameter for the curve.</param>
        /// <returns name="Curves">new created curves</returns> 
        [Obsolete]
        protected static List<Autodesk.DesignScript.Geometry.Curve> SplitCurveByParameterDynamo(Autodesk.DesignScript.Geometry.Curve dynamoCurve, double splitParam)
        {
            return dynamoCurve.SplitByParameter(splitParam).ToList();
        }

        #endregion

        ///// <summary>
        ///// Function used to compute the geometry information of the Rebar element during document regeneration.
        ///// Geometry information includes: 
        /////  1. Graphical representation of the Rebar or Rebar Set; 
        /////  2. Hook placement;
        /////  3. Distribution Path for MRA;
        /////  
        ///// </summary>
        ///// <param name="data">Class used to pass information from the external application to the internal Rebar Element.
        ///// Interfaces with the Rebar Element and exposes information needed for geometric calculation during regeneration,
        ///// such as constrained geometry, state of changed input information, etc.
        ///// Receives the result of the custom constraint calculation and 
        ///// updates the element after the entire function finished successfully.
        ///// </param>
        ///// <returns> true if geometry generation was completed successfully, false otherwise</returns>
        //public bool GenerateCurves(RebarCurvesData data)
        //{
        //    // used to store the faces and transforms used in generation of curves
        //    TargetFace firstFace = new TargetFace();
        //    TargetFace secondFace = new TargetFace();
        //    TargetFace thirdFace = new TargetFace();
        //    //iterate through the available constraints and extract the needed information
        //    IList<RebarConstraint> constraints = data.GetRebarUpdateCurvesData().GetCustomConstraints();
        //    foreach (RebarConstraint constraint in constraints)
        //    {
        //        if (constraint.NumberOfTargets > 1)
        //            return false;
        //        Transform tempTrf = Transform.Identity;
        //        double dfOffset = 0;
        //        if (!getOffsetFromConstraintAtTarget(data.GetRebarUpdateCurvesData(), constraint, 0, out dfOffset))
        //            return false;

        //        switch ((BarHandle)constraint.GetCustomHandleTag())
        //        {
        //            case BarHandle.FirstHandle:
        //                {
        //                    Face face = constraint.GetTargetHostFaceAndTransform(0, tempTrf);
        //                    firstFace = new TargetFace() { Face = face, Transform = tempTrf, Offset = dfOffset };
        //                    break;
        //                }
        //            case BarHandle.SecondHandle:
        //                {
        //                    Face face = constraint.GetTargetHostFaceAndTransform(0, tempTrf);
        //                    secondFace = new TargetFace() { Face = face, Transform = tempTrf, Offset = dfOffset };
        //                    break;
        //                }
        //            case BarHandle.ThirdHandle:
        //                {
        //                    Face face = constraint.GetTargetHostFaceAndTransform(0, tempTrf);
        //                    thirdFace = new TargetFace() { Face = face, Transform = tempTrf, Offset = dfOffset };
        //                    break;
        //                }
        //            default:
        //                break;
        //        }
        //    }
        //    // check if all the input is present for the calculation, otherwise return error(false).
        //    if (firstFace.Face == null || secondFace.Face == null || thirdFace.Face == null)
        //        return false;

        //    Rebar thisBar = getCurrentRebar(data.GetRebarUpdateCurvesData());
        //    CurveElement selectedCurve = null;
        //    //if a curve elem is selected, we override the geometry we get from the intersections and use the selected curve to create our bar geometries
        //    selectedCurve = getSelectedCurveElement(thisBar, data.GetRebarUpdateCurvesData());
        //    //used to store the resulting curves
        //    List<Curve> curves = new List<Curve>();
        //    Curve originalBar = null;
        //    Curve singleBar = getOffsetCurveAtIntersection(firstFace, secondFace);
        //    if (selectedCurve != null)
        //    {
        //        Transform trf = Transform.CreateTranslation(singleBar.GetEndPoint(0) - selectedCurve.GeometryCurve.GetEndPoint(0));
        //        originalBar = singleBar;
        //        singleBar = selectedCurve.GeometryCurve.CreateTransformed(trf);
        //    }
        //    //we can't make any more bars without the first one.
        //    if (singleBar == null)
        //        return false;

        //    // check the layout rule to see if we need to create more bars
        //    // for this example, any rule that is not single will generate bars in the same way, 
        //    // creating them at an equal distance to each other, based only on number of bars
        //    RebarLayoutRule layout = data.GetRebarUpdateCurvesData().GetLayoutRule();
        //    switch (layout)
        //    {
        //        case RebarLayoutRule.Single:// first bar creation: intersect first face with second face to get a curve
        //            curves.Add(singleBar);
        //            break;
        //        case RebarLayoutRule.FixedNumber:
        //        case RebarLayoutRule.NumberWithSpacing:
        //        case RebarLayoutRule.MaximumSpacing:
        //        case RebarLayoutRule.MinimumClearSpacing:
        //            curves.Add(singleBar);
        //            Curve lastBar = getOffsetCurveAtIntersection(firstFace, thirdFace);// create last bar

        //            // keep the curves pointing in the same direction
        //            var firstBar = (selectedCurve != null) ? originalBar : singleBar;
        //            if (lastBar == null || !alignBars(ref firstBar, ref lastBar))
        //                return false;
        //            if (selectedCurve != null)
        //            {
        //                Transform trf = Transform.CreateTranslation(lastBar.GetEndPoint(0) - selectedCurve.GeometryCurve.GetEndPoint(0));
        //                lastBar = selectedCurve.GeometryCurve.CreateTransformed(trf);
        //            }

        //            if (!generateSet(singleBar, lastBar, layout,
        //                            data.GetRebarUpdateCurvesData().GetBarsNumber(),
        //                            data.GetRebarUpdateCurvesData().Spacing, ref curves, selectedCurve == null ? null : selectedCurve.GeometryCurve))
        //                return false;
        //            curves.Add(lastBar);
        //            break;
        //        default:
        //            break;
        //    }

        //    // check if any curves were created 
        //    if (curves.Count <= 0)
        //        return false;

        //    // create the distribution path for the bars that were created;
        //    // one single bar will not have a distribution path.
        //    List<Curve> distribPath = new List<Curve>();
        //    for (int ii = 0; ii < curves.Count - 1; ii++)
        //        distribPath.Add(Line.CreateBound(curves[ii].Evaluate(0.5, true), curves[ii + 1].Evaluate(0.5, true)));
        //    // set distribution path if we have a path created
        //    if (distribPath.Count > 0)
        //        data.SetDistributionPath(distribPath);

        //    // add each curve as separate bar in the set.
        //    for (int ii = 0; ii < curves.Count; ii++)
        //    {
        //        List<Curve> barCurve = new List<Curve>();
        //        barCurve.Add(curves[ii]);
        //        data.AddBarGeometry(barCurve);

        //        // set the hook normals for each bar added
        //        // important!: hook normals set here will be reset if bar geometry is changed on TrimExtendCurves
        //        // so they need to be recalculated then.
        //        for (int i = 0; i < 2; i++)
        //        {
        //            XYZ normal = computeNormal(curves[ii], firstFace, i);
        //            if (normal != null && !normal.IsZeroLength())
        //                data.GetRebarUpdateCurvesData().SetHookPlaneNormalForBarIdx(i, ii, normal);
        //        }
        //    }
        //    return true;
        //}

        #region "copy from Github"
        ////https://github.com/tt-acm/DynamoForRebar/blob/48d2b9d40dfa00aa79f470799de58047396bf0ef/src/DynamoRebar/Nodes.cs#L241
        ///// <summary>
        ///// Cuts a set of Rebars by Plane
        ///// </summary>
        ///// <param name="plane">Plane to cut by</param>
        ///// <param name="rebarContainerElement">Rebar Container</param>
        ///// <param name="firstPart">Return the first or the last part of the splitted elements</param>
        //[Obsolete]
        //public static void Cut(Autodesk.Revit.DB.Surface plane, global::Revit.Elements.Element rebarContainerElement, bool firstPart)
        //{
        //    // Get Rebar Container Element
        //    RebarContainer rebarContainer = (RebarContainer)rebarContainerElement.InternalElement;

        //    // Get the active Document
        //    Autodesk.Revit.DB.Document document = DocumentManager.Instance.CurrentDBDocument;

        //    // Open a new Transaction
        //    TransactionManager.Instance.EnsureInTransaction(document);

        //    // Get all single Rebar elements from the container
        //    List<RebarContainerItem> rebars = rebarContainer.ToList();

        //    // Walk through all rebar elements
        //    foreach (RebarContainerItem rebar in rebars)
        //    {
        //        // Buffer Rebar properties for recreation
        //        RebarBarType barType = (RebarBarType)document.GetElement(rebar.BarTypeId);
        //        RebarHookType hookTypeStart = (RebarHookType)document.GetElement(rebar.GetHookTypeId(0));
        //        RebarHookType hookTypeEnd = (RebarHookType)document.GetElement(rebar.GetHookTypeId(1));
        //        RebarHookOrientation hookOrientationStart = rebar.GetHookOrientation(0);
        //        RebarHookOrientation hookOrientationEnd = rebar.GetHookOrientation(1);

        //        // create a list to store the remaining part of the curve after cutting it
        //        List<Autodesk.Revit.DB.Curve> result = new List<Autodesk.Revit.DB.Curve>();

        //        // get the center line curves of the rebar elements
        //        foreach (Autodesk.Revit.DB.Curve curve in rebar.GetCenterlineCurves(false, true, true))
        //        {
        //            // if the curve is a line or an arc consider it being valid
        //            if (curve.GetType() == typeof(Autodesk.Revit.DB.Line) || curve.GetType() == typeof(Autodesk.Revit.DB.Arc))
        //            {
        //                // Get a DesignScript Curve from the Revit curve
        //                Autodesk.DesignScript.Geometry.Curve geocurve = curve.ToProtoType();

        //                // Intersect the selected plane with the curve
        //                //foreach (Geometry geometry in plane.Intersect(geocurve))
        //                foreach (Geometry geometry in geocurve.Intersect(plane))
        //                {
        //                    // if the intersection is a point
        //                    if (geometry.GetType() == typeof(Autodesk.DesignScript.Geometry.Point))
        //                    {
        //                        // Get the closest point to the intersection on the curve
        //                        Autodesk.DesignScript.Geometry.Point p = geocurve.ClosestPointTo((Autodesk.DesignScript.Geometry.Point)geometry);

        //                        // Split the curve at this point
        //                        Autodesk.DesignScript.Geometry.Curve[] curves = geocurve.SplitByParameter(geocurve.ParameterAtPoint(p));

        //                        // If the curve has been split into two parts
        //                        if (curves.Length == 2)
        //                        {
        //                            // return the first or the second part of the splitted curve
        //                            if (firstPart)
        //                                result.Add(curves[0].ToRevitType());
        //                            else
        //                                result.Add(curves[1].ToRevitType());
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        // If the result has some elements, create a new rebar container from those curves
        //        // using the same properties as the initial one.
        //        if (result.Count > 0)
        //        {
        //            rebar.SetFromCurves(RebarStyle.Standard, barType, hookTypeStart, hookTypeEnd, rebar.Normal, result, hookOrientationStart, hookOrientationEnd, true, false);
        //        }
        //    }

        //    // Commit and Dispose the transaction
        //    TransactionManager.Instance.TransactionTaskDone();
        //}




        ///// <summary>
        ///// Cuts a set of Rebars by Plane
        ///// </summary>
        ///// <param name="plane">Plane to cut by</param>
        ///// <param name="rebarContainerElement">Rebar Container</param>
        ///// <param name="firstPart">Return the first or the last part of the splitted elements</param>
        //public static void Cut(Surface plane, Revit.Elements.Element rebarContainerElement, bool firstPart)
        //{
        //    // Get Rebar Container Element
        //    Autodesk.Revit.DB.Structure.RebarContainer rebarContainer = (Autodesk.Revit.DB.Structure.RebarContainer)rebarContainerElement.InternalElement;

        //    // Get the active Document
        //    Autodesk.Revit.DB.Document document = DocumentManager.Instance.CurrentDBDocument;

        //    // Open a new Transaction
        //    TransactionManager.Instance.EnsureInTransaction(document);

        //    // Get all single Rebar elements from the container
        //    List<Autodesk.Revit.DB.Structure.RebarContainerItem> rebars = rebarContainer.ToList();

        //    // Walk through all rebar elements
        //    foreach (Autodesk.Revit.DB.Structure.RebarContainerItem rebar in rebars)
        //    {
        //        // Buffer Rebar properties for recreation
        //        RVT.Structure.RebarBarType barType = (RVT.Structure.RebarBarType)document.GetElement(rebar.BarTypeId);
        //        RVT.Structure.RebarHookType hookTypeStart = (RVT.Structure.RebarHookType)document.GetElement(rebar.GetHookTypeId(0));
        //        RVT.Structure.RebarHookType hookTypeEnd = (RVT.Structure.RebarHookType)document.GetElement(rebar.GetHookTypeId(1));
        //        RVT.Structure.RebarHookOrientation hookOrientationStart = rebar.GetHookOrientation(0);
        //        RVT.Structure.RebarHookOrientation hookOrientationEnd = rebar.GetHookOrientation(1);

        //        // create a list to store the remaining part of the curve after cutting it
        //        List<RVT.Curve> result = new List<RVT.Curve>();

        //        // get the center line curves of the rebar elements
        //        foreach (RVT.Curve curve in rebar.GetCenterlineCurves(false, true, true))
        //        {
        //            // if the curve is a line or an arc consider it being valid
        //            if (curve.GetType() == typeof(RVT.Line) || curve.GetType() == typeof(RVT.Arc))
        //            {
        //                // Get a DesignScript Curve from the Revit curve
        //                Curve geocurve = curve.ToProtoType();

        //                // Intersect the selected plane with the curve
        //                foreach (Geometry geometry in plane.Intersect(geocurve))
        //                {
        //                    // if the intersection is a point
        //                    if (geometry.GetType() == typeof(Point))
        //                    {
        //                        // Get the closest point to the intersection on the curve
        //                        Point p = geocurve.ClosestPointTo((Point)geometry);

        //                        // Split the curve at this point
        //                        Curve[] curves = geocurve.SplitByParameter(geocurve.ParameterAtPoint(p));

        //                        // If the curve has been split into two parts
        //                        if (curves.Length == 2)
        //                        {
        //                            // return the first or the second part of the splitted curve
        //                            if (firstPart)
        //                                result.Add(curves[0].ToRevitType());
        //                            else
        //                                result.Add(curves[1].ToRevitType());
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        // If the result has some elements, create a new rebar container from those curves
        //        // using the same properties as the initial one.
        //        if (result.Count > 0)
        //        {
        //            rebar.SetFromCurves(RVT.Structure.RebarStyle.Standard, barType, hookTypeStart, hookTypeEnd, rebar.Normal, result, hookOrientationStart, hookOrientationEnd, true, false);
        //        }
        //    }

        //    // Commit and Dispose the transaction
        //    TransactionManager.Instance.TransactionTaskDone();
        //}

        #endregion

    }
}

using Autodesk.DesignScript.Runtime;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using Dynamo.Graph.Nodes;
using Revit.Elements;
using Revit.GeometryConversion;
using RevitServices.Persistence;
using RevitServices.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Geometry;
using System.Xml.Linq;

namespace DynamoLab.Revit.Elements
{
    /// <summary>
    /// Utility class that contains methods of reinforment rebar creation.
    /// </summary>
    public class StructuralRebar
    {
        private StructuralRebar() { }


        /// <summary>
        /// the family instance to places rebar on
        /// </summary>
        protected Autodesk.Revit.DB.FamilyInstance m_hostObject;

        /// <summary>
        /// a set to store all the rebar types
        /// </summary>
        protected List<RebarBarType> m_rebarBarTypes = new List<RebarBarType>();

        /// <summary>
        /// a list to store all the hook types
        /// </summary>
        protected List<RebarHookType> m_rebarHookTypes = new List<RebarHookType>();

        /// <summary>
        /// a set to store all the rebar types name
        /// </summary>
        protected List<string> m_rebarBarTypeNames = new List<string>();

        /// <summary>
        /// a list to store all the hook types name
        /// </summary>
        protected List<string> m_rebarHookTypeNames = new List<string>();

        /// <summary>
        /// Show all the rebar types in revit
        /// </summary>
        public IList<RebarBarType> RebarBarTypes
        {
            get
            {
                return m_rebarBarTypes;
            }
        }

        /// <summary>
        /// Show all the rebar hook types in revit
        /// </summary>
        public IList<RebarHookType> RebarHookTypes
        {
            get
            {
                return m_rebarHookTypes;
            }
        }

        /// <summary>
        /// Show all the rebar types' name in revit
        /// </summary>
        public List<string> RebarBarTypeNames
        {
            get
            {
                return m_rebarBarTypeNames;
            }
        }

        /// <summary>
        /// Show all the rebar hook types' name in revit
        /// </summary>
        public List<string> RebarHookTypeNames
        {
            get
            {
                return m_rebarHookTypeNames;
            }
        }

        /// <summary>
        /// a list to store all the Rebar Style
        /// </summary>
        protected List<RebarStyle> m_rebarStyles = new List<RebarStyle>();

        /// <summary>
        /// Show all the rebar style in revit
        /// </summary>
        public IList<RebarStyle> RebarStyles
        {
            get
            {
                return m_rebarStyles;
            }
        }

        /// <summary>
        /// a list to store all the Rebar Hook Orientation
        /// </summary>
        protected List<RebarHookOrientation> m_rebarHookOrientations = new List<RebarHookOrientation>();

        /// <summary>
        /// Show all the rebar Hook Orientation in revit
        /// </summary>
        public IList<RebarHookOrientation> RebarHookOrientations
        {
            get
            {
                return m_rebarHookOrientations;
            }
        }

        /// <summary>
        /// a list to store all the Rebar Shape
        /// </summary>
        protected List<RebarShape> m_rebarShapes = new List<RebarShape>();

        /// <summary>
        /// Show all the rebar shape in revit
        /// </summary>
        public IList<RebarShape> RebarShapes
        {
            get
            {
                return m_rebarShapes;
            }
        }


        //https://www.revitapidocs.com/2024/206e9dc6-5fc1-c0a8-6c76-d5a53ee93a39.htm
        /// <summary>
        /// The constructor of RebarBarType,RebarHookType
        /// </summary>
        ////public static methods that return the class type will appear as constructor nodes in Dynamo, identified with a green + icon
        public static StructuralRebar GetRebarInfor()
        {
            //Get application and document objects
            Document doc = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            var rebarInfor = new StructuralRebar();

            // Get all the rebar types in revit
            if (rebarInfor.GetRebarTypes(doc))
            {
                foreach (RebarBarType rebarBarType in rebarInfor.m_rebarBarTypes)
                {
                    string rebarBarTypeName = rebarBarType.Name;
                    rebarInfor.m_rebarBarTypeNames.Add(rebarBarTypeName);
                }
            }
            else
            {
                throw new Exception("Can't get any rebar type from revit.");
            }

            // Get all the rebar hook types in revit
            if (rebarInfor.GetHookTypes(doc))
            {
                foreach (RebarHookType rebarHookType in rebarInfor.m_rebarHookTypes)
                {
                    string rebarHookTypeName = rebarHookType.Name;
                    rebarInfor.m_rebarHookTypeNames.Add(rebarHookTypeName);
                }
            }
            else
            {
                throw new Exception("Can't get any rebar hook type from revit.");
            }

            // Get all the rebar style
            if (!rebarInfor.GetRebarStyle_ZTN())
            {
                throw new Exception("Can't get any rebar style.");
            }

            // Get all the rebar Hook orientation
            if (!rebarInfor.GetRebarHookOrientation_ZTN())
            {
                throw new Exception("Can't get any rebar Hook orientation.");
            }

            // Get all the rebar Shape
            if (!rebarInfor.GetRebarShapes(doc))
            {
                throw new Exception("Can't get any rebar shape.");
            }

            return rebarInfor;
        }


        #region "Create Rebar"
        /// <summary>
        /// A wrap fuction which used to create the reinforcement.
        /// </summary>
        /// <param name="columnDynamo">The host element of the rebar</param>
        /// <param name="barType">The element of RebarBarType</param>
        /// <param name="hookType">The element of RebarHookType</param>
        /// <returns name="Rebar">new created Rebar</returns> 
        [NodeCategory("Create")]
        public static Rebar CreateRebar_ZTN(global::Revit.Elements.Element columnDynamo, RebarBarType barType, RebarHookType hookType)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.FamilyInstance column = (Autodesk.Revit.DB.FamilyInstance)columnDynamo.InternalElement;

            // Define the rebar geometry information - Line rebar
            LocationPoint location = column.Location as LocationPoint;
            XYZ origin = location.Point;
            XYZ normal = new XYZ(1, 0, 0);
            // create rebar 9' long
            XYZ rebarLineEnd = new XYZ(origin.X, origin.Y, origin.Z + 9);
            Autodesk.Revit.DB.Line rebarLine = Autodesk.Revit.DB.Line.CreateBound(origin, rebarLineEnd);

            // Create the line rebar
            IList<Autodesk.Revit.DB.Curve> curves = new List<Autodesk.Revit.DB.Curve>();
            curves.Add(rebarLine);

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            Rebar rebar = Rebar.CreateFromCurves(dynamoDocument, Autodesk.Revit.DB.Structure.RebarStyle.Standard, barType, hookType, hookType, column, normal, curves, RebarHookOrientation.Right, RebarHookOrientation.Left, true, true);

            if (null != rebar)
            {
                // set specific layout for new rebar as fixed number, with 10 bars, distribution path length of 1.5'
                // with bars of the bar set on the same side of the rebar plane as indicated by normal
                // and both first and last bar in the set are shown
                rebar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(10, 1.5, true, true, true);
                ShowRebar3d(rebar);
            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return rebar;
        }

        //https://www.revitapidocs.com/2020.1/aadf041a-9564-e76e-4bc4-db4a3f861d84.htm


        //https://www.revitapidocs.com/2023/b020c9d5-6026-b9fa-7e23-f6a7ec2cede3.htm
        /// <summary>
        /// Creates a new instance of a shape driven Rebar element within the project.
        /// </summary>
        /// <param name="style">Type: Autodesk.Revit.DB.Structure RebarBarType, 
        /// The usage of the bar, "Autodesk.Revit.DB.Structure.RebarStyle.Standard" or "stirrup/tie"</param>
        /// <param name="barType">A RebarBarType element that defines bar diameter, bend radius and material of the rebar.</param>
        /// <param name="startHookType">A RebarHookType element that defines the hook for the start of the bar. 
        /// If this parameter is a null reference ( Nothing in Visual Basic) , it means to create a rebar with no hook.</param>
        /// <param name="endHookType">A RebarHookType element that defines the hook for the end of the bar. 
        /// If this parameter is a null reference ( Nothing in Visual Basic) , it means to create a rebar with no hook.</param>
        /// <param name="dynamoHost">The element to which the rebar belongs. The element must support rebar hosting.</param>
        /// <param name="normDynamo">The normal to the plane that the rebar curves lie on.</param>
        /// <param name="curvesDynamo">An array of curves that define the shape of the rebar curves. They must belong to 
        /// the plane defined by the normal and origin. Bends and hooks should not be included in the array of curves..</param>
        /// <param name="startHookOrient">Defines the orientation of the hook plane at the start of the rebar with respect to 
        /// the orientation of the first curve and the plane normal,RebarHookOrientation.Right.</param>
        /// <param name="endHookOrient">Defines the orientation of the hook plane at the end of the rebar with respect to the 
        /// orientation of the last curve and the plane normal,RebarHookOrientation.Left.</param>
        /// <param name="useExistingShapeIfPossible">Attempts to assign a RebarShape from those existing in the document. 
        /// If no shape matches, NewRebar returns or creates a new shape, according to the parameter createNewShape.</param>
        /// <param name="createNewShape">Creates a shape in the document to match the curves, hooks, and style specified, 
        /// and assigns it to the new rebar instance.</param>
        /// <returns name="Rebar">new created Rebar</returns> 
        [NodeCategory("Create")]  //works because we addusing Dynamo.Graph.Nodes; Reference from DynamoVisualProgramming.Core at NuGet Package
        public static global::Revit.Elements.Element CreateRebarFromCurve(RebarStyle style, RebarBarType barType, RebarHookType startHookType, RebarHookType endHookType,
            global::Revit.Elements.Element dynamoHost, Autodesk.DesignScript.Geometry.Vector normDynamo, IList<Autodesk.DesignScript.Geometry.Curve> curvesDynamo, RebarHookOrientation startHookOrient,
            RebarHookOrientation endHookOrient, bool useExistingShapeIfPossible = true, bool createNewShape = true)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.FamilyInstance host = (Autodesk.Revit.DB.FamilyInstance)dynamoHost.InternalElement;

            //UnWrap: get Revit normal vector to plane from the Dynamo-wrapped normal to plane
            Autodesk.Revit.DB.XYZ norm = normDynamo.ToRevitType();

            IList<Autodesk.Revit.DB.Curve> curves = new List<Autodesk.Revit.DB.Curve>();
            foreach (Autodesk.DesignScript.Geometry.Curve curveDynamo in curvesDynamo)
            {
                //UnWrap: get Revit object from the Dynamo-wrapped object   
                Autodesk.Revit.DB.Curve curve = curveDynamo.ToRevitType();
                curves.Add(curve);
            }

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            Rebar rebar = Rebar.CreateFromCurves(dynamoDocument, style, barType, startHookType, endHookType, host, norm, curves, startHookOrient, endHookOrient, useExistingShapeIfPossible, createNewShape);

            if (null != rebar)
            {
                // set specific layout for new rebar as fixed number, with 10 bars, distribution path length of 1.5'
                // with bars of the bar set on the same side of the rebar plane as indicated by normal
                // and both first and last bar in the set are shown
                //rebar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(10, 1.5, true, true, true);
                ShowRebar3d(rebar);
            }

            global::Revit.Elements.Element dynamoRebar = rebar.ToDSType(true);
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();
            return dynamoRebar;
        }


        //https://www.revitapidocs.com/2023/5e58e3f3-dea6-79cb-9de4-475e6fe5c28b.htm
        /// <summary>
        /// Creates a new shape driven Rebar, as an instance of a RebarShape.
        /// </summary>
        /// <param name="rebarShape">A RebarShape element that defines the shape of the rebar.</param>
        /// <param name="barType">A RebarBarType element that defines bar diameter, bend radius and material of the rebar.</param>
        /// <param name="dynamoHost">The element to which the rebar belongs. The element must support rebar hosting.</param>
        /// <param name="originDynamo">The lower-left corner of the shape's bounding box will be placed at this point in the project.</param>
        /// <param name="xVecDynamo">The x-axis in the shape definition will be mapped to this direction in the project.</param>
        /// <param name="yVecDynamo">The y-axis in the shape definition will be mapped to this direction in the project.</param>
        /// <returns name="Rebar">new created Rebar</returns> 
        [NodeCategory("Create")]  //works because we addusing Dynamo.Graph.Nodes; Reference from DynamoVisualProgramming.Core at NuGet Package
        public static global::Revit.Elements.Element CreateRebarFromShape(RebarShape rebarShape, RebarBarType barType, global::Revit.Elements.Element dynamoHost,
            Autodesk.DesignScript.Geometry.Point originDynamo, Autodesk.DesignScript.Geometry.Vector xVecDynamo, Autodesk.DesignScript.Geometry.Vector yVecDynamo)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.FamilyInstance host = (Autodesk.Revit.DB.FamilyInstance)dynamoHost.InternalElement;

            //UnWrap: get Revit point from the Dynamo-wrapped point
            Autodesk.Revit.DB.XYZ origin = originDynamo.ToRevitType();

            //UnWrap: get Revit vector from the Dynamo-wrapped vector
            Autodesk.Revit.DB.XYZ xVec = xVecDynamo.ToRevitType();
            Autodesk.Revit.DB.XYZ yVec = yVecDynamo.ToRevitType();

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            Rebar rebar = Rebar.CreateFromRebarShape(dynamoDocument, rebarShape, barType, host, origin, xVec, yVec);

            if (null != rebar)
            {
                // set specific layout for new rebar as fixed number, with 10 bars, distribution path length of 1.5'
                // with bars of the bar set on the same side of the rebar plane as indicated by normal
                // and both first and last bar in the set are shown
                //rebar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(10, 1.5, true, true, true);
                ShowRebar3d(rebar);
            }

            global::Revit.Elements.Element dynamoRebar = rebar.ToDSType(true);
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();
            return dynamoRebar;
        }


        //https://www.revitapidocs.com/2023/10ddc28e-a410-5f29-6fe9-d4b73f917c54.htm
        /// <summary>
        /// Creates a new instance of a shape driven Rebar element within the project.
        /// The instance will have the default shape parameters from the RebarShape. 
        /// If the RebarShapeDefinesHooks flag in ReinforcementSettings has been set to true, then both the curves and hooks must match the RebarShape definition. 
        /// Otherwise, the hooks can be different than the defaults specified in the RebarShape
        /// </summary>
        /// <param name="rebarShape">A RebarShape element that defines the shape of the rebar. A RebarShape element matches curves and hooks. 
        /// A RebarShape element provides RebarStyle of the rebar.</param>
        /// <param name="barType">A RebarBarType element that defines bar diameter, bend radius and material of the rebar.</param>
        /// <param name="startHookType">A RebarHookType element that defines the hook for the start of the bar. 
        /// If this parameter is a null reference ( Nothing in Visual Basic) , it means to create a rebar with no hook.</param>
        /// <param name="endHookType">A RebarHookType element that defines the hook for the end of the bar. 
        /// If this parameter is a null reference ( Nothing in Visual Basic) , it means to create a rebar with no hook.</param>
        /// <param name="dynamoHost">The element to which the rebar belongs. The element must support rebar hosting.</param>
        /// <param name="normDynamo">The normal to the plane that the rebar curves lie on.</param>
        /// <param name="curvesDynamo">An array of curves that define the shape of the rebar curves. They must belong to 
        /// the plane defined by the normal and origin. Bends and hooks should not be included in the array of curves..</param>
        /// <param name="startHookOrient">Defines the orientation of the hook plane at the start of the rebar with respect to 
        /// the orientation of the first curve and the plane normal,RebarHookOrientation.Right.</param>
        /// <param name="endHookOrient">Defines the orientation of the hook plane at the end of the rebar with respect to the 
        /// orientation of the last curve and the plane normal,RebarHookOrientation.Left.</param>
        /// <returns name="Rebar">new created Rebar</returns> 
        [NodeCategory("Create")]  //works because we addusing Dynamo.Graph.Nodes; Reference from DynamoVisualProgramming.Core at NuGet Package
        public static global::Revit.Elements.Element CreateRebarFromCurveAndShape(RebarShape rebarShape, RebarBarType barType, RebarHookType startHookType, RebarHookType endHookType,
            global::Revit.Elements.Element dynamoHost, Autodesk.DesignScript.Geometry.Vector normDynamo, IList<Autodesk.DesignScript.Geometry.Curve> curvesDynamo, RebarHookOrientation startHookOrient,
            RebarHookOrientation endHookOrient)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.FamilyInstance host = (Autodesk.Revit.DB.FamilyInstance)dynamoHost.InternalElement;

            //UnWrap: get Revit normal vector to plane from the Dynamo-wrapped normal to plane
            Autodesk.Revit.DB.XYZ norm = normDynamo.ToRevitType();

            IList<Autodesk.Revit.DB.Curve> curves = new List<Autodesk.Revit.DB.Curve>();
            foreach (Autodesk.DesignScript.Geometry.Curve curveDynamo in curvesDynamo)
            {
                //UnWrap: get Revit object from the Dynamo-wrapped object   
                Autodesk.Revit.DB.Curve curve = curveDynamo.ToRevitType();
                curves.Add(curve);
            }

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            Rebar rebar = Rebar.CreateFromCurvesAndShape(dynamoDocument, rebarShape, barType, startHookType, endHookType, host, norm, curves, startHookOrient, endHookOrient);

            if (null != rebar)
            {
                // set specific layout for new rebar as fixed number, with 10 bars, distribution path length of 1.5'
                // with bars of the bar set on the same side of the rebar plane as indicated by normal
                // and both first and last bar in the set are shown
                //rebar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(10, 1.5, true, true, true);
                ShowRebar3d(rebar);
            }

            global::Revit.Elements.Element dynamoRebar = rebar.ToDSType(true);
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();
            return dynamoRebar;
        }
        #endregion


        #region "get properties from selected rebar"
        //https://www.revitapidocs.com/2023/e3f78f8f-27bb-e169-c06c-e2afcf00f69e.htm
        /// <summary>
        /// Get selected rebar properties
        /// </summary>
        /// <returns name="RebarShape"> Returns the RebarShape element that defines the shape of the rebar.</returns> 
        /// <returns name="RebarShapeName"> Returns the RebarShape element's name</returns> 
        /// <returns name="RebarStyle"> Whether the shape represents a standard bar or a stirrup.</returns> 
        /// <returns name="RebarBarType"> Returns the rebar element's type.</returns> 
        /// <returns name="RebarBarTypeName"> Returns the rebar element's type name</returns> 
        /// <returns name="StartRebarHookType">	Get the RebarHookType at the start of the rebar.</returns> 
        /// <returns name="StartRebarHookTypeName"> Get the RebarHookType'name at the start of the rebar</returns> 
        /// <returns name="EndRebarHookType"> Get the RebarHookType at the end of the rebar</returns> 
        /// <returns name="EndRebarHookTypeName"> Get the RebarHookType'name at the end of the rebar</returns> 
        /// <returns name="RebarHostElement">	get rebar host elment, for example structural column or structural beam.</returns> 
        /// <returns name="Normal"> get the center line curves of the rebar elements. Note: for rebar set, use normal vector from GetRebarSetInformation</returns> 
        /// <returns name="CurveList"> get the center line curves of the rebar elements.</returns> 
        /// <returns name="StartRebarHookOrientation"> Returns the orientation of the hook plane at the start of the rebar 
        /// with respect to the orientation of the first or the last curve and the plane normal.</returns> 
        /// <returns name="EndRebarHookOrientation"> Returns the orientation of the hook plane at the end of the rebar 
        /// with respect to the orientation of the first or the last curve and the plane normal.</returns> 
        /// <returns name="MajorSegmentIndex"> The normal to the plane that the rebar curves lie on.</returns> 
        /// <returns name="FreeFormOrShapeDriven"> Returns true if the rebar is free form and false if shape driven.</returns> 
        [MultiReturn(new[] { "RebarShape", "RebarShapeName", "RebarStyle", "RebarBarType", "RebarBarTypeName", "StartRebarHookType", "StartRebarHookTypeName","EndRebarHookType",
             "EndRebarHookTypeName" , "RebarHostElement", "Normal", "CurveList","StartRebarHookOrientation", "EndRebarHookOrientation","MajorSegmentIndex","FreeFormOrShapeDriven"})]
        public static object GetRebarProperties_ZTN(global::Revit.Elements.Element dynamoRebar)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            Autodesk.Revit.DB.Document doc = DocumentManager.Instance.CurrentDBDocument;
            Rebar revitRebar = (Rebar)dynamoRebar.InternalElement;

            //https://www.revitapidocs.com/2023/6edc946f-d8a3-ee78-adbb-7d5359501ed3.htm
            RebarShape rebarShape = (RebarShape)doc.GetElement(revitRebar.GetShapeId());
            string rebarShapeName = rebarShape.Name;

            //https://www.revitapidocs.com/2023/ec72e836-ea5a-8d74-40f0-55344ae095bd.htm
            RebarStyle rebarStyle = rebarShape.RebarStyle;

            //https://www.revitapidocs.com/2023/a9ac65a6-29e6-25e5-caca-502e21385f47.htm
            //Second mothod to get RebarStyle
            //Autodesk.Revit.DB.Parameter rebarStyleParameter = revitRebar.get_Parameter(BuiltInParameter.REBAR_ELEM_HOOK_STYLE);
            //RebarStyle rebarStyle ;
            //int id = rebarStyleParameter.AsInteger();
            //if (id == 0)
            //{
            //    rebarStyle = RebarStyle.Standard;
            //}
            //else
            //{
            //    rebarStyle = RebarStyle.StirrupTie;
            //}

            //get RebarBarType from selected Rebar
            //https://www.revitapidocs.com/2023/cc66ca8e-302e-f072-edca-d847bcf14c86.htm
            RebarBarType rebarBarType = (RebarBarType)doc.GetElement(revitRebar.GetTypeId());
            string rebarBartypeName = rebarBarType.Name;

            ////https://www.revitapidocs.com/2023/016d53d9-0ef5-99d1-b12f-089f04df3952.htm
            ////0 for the start hook, 1 for the end hook.
            RebarHookType startRebarHookType = (RebarHookType)doc.GetElement(revitRebar.GetHookTypeId(0));
            string startRebarHookTypeName;
            if (startRebarHookType == null)
            {
                startRebarHookTypeName = null;
            }
            else
            {
                startRebarHookTypeName = startRebarHookType.Name;
            }
            RebarHookType endRebarHookType = (RebarHookType)doc.GetElement(revitRebar.GetHookTypeId(1));
            string endRebarHookTypeName;
            if (endRebarHookType == null)
            {
                endRebarHookTypeName = null;
            }
            else
            {
                endRebarHookTypeName = endRebarHookType.Name;
            }


            //https://www.revitapidocs.com/2023/0aabc992-1723-9f78-aff7-ef9760f8a64b.htm
            //0 for the start hook, 1 for the end hook.
            RebarHookOrientation startRebarHookOrientation = (RebarHookOrientation)revitRebar.GetHookOrientation(0);
            RebarHookOrientation endRebarHookOrientation = (RebarHookOrientation)revitRebar.GetHookOrientation(1);

            //get rebar host elment, for example structural column or structural beam
            ElementId host = revitRebar.GetHostId();
            Autodesk.Revit.DB.Element rebarHost = dynamoDocument.GetElement(host);
            global::Revit.Elements.Element rebarHostElement = rebarHost.ToDSType(false);

            //https://www.revitapidocs.com/2023/70fd7426-f4a4-591c-8c06-3c18dda45e7d.htm
            //get the center line curves of the rebar elements
            List<Autodesk.Revit.DB.Curve> centerCurves = revitRebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0).ToList();
            //Change from Revit curve to dynamo curve
            List<Autodesk.DesignScript.Geometry.Curve> curveList = new List<Autodesk.DesignScript.Geometry.Curve>();
            foreach (Autodesk.Revit.DB.Curve curve in centerCurves)
            {
                // Get a DesignScript Curve from the Revit curve
                Autodesk.DesignScript.Geometry.Curve geocurve = curve.ToProtoType();
                curveList.Add(geocurve);
            }

            XYZ normalRevit = DynamoLab.Revit.TroubleShooting.RebarBroken.GetRebarCurvePlaneNormal(centerCurves);
            Autodesk.DesignScript.Geometry.Vector normal = normalRevit.ToVector();

            // Get the shape definition
            //https://www.revitapidocs.com/2023/80019bb8-76a4-f6e1-476d-2f9992286adb.htm           
            RebarShapeDefinitionBySegments shapeDefinition = (RebarShapeDefinitionBySegments)rebarShape.GetRebarShapeDefinition();
            int majorSegmentIndex = shapeDefinition.MajorSegmentIndex;

            //https://www.revitapidocs.com/2023/cf39a5b8-d7f3-d073-0120-358dd3afab21.htm
            //Returns true if the rebar is free form and false if shape driven.
            bool freeFormOrShapeDriven = revitRebar.IsRebarFreeForm();

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, object> rebarPropertyeDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                //{ "specTypeIdProperty",specTypeIdPropertyList},
                { "RebarShape",rebarShape},
                { "RebarShapeName",rebarShapeName},

                { "RebarStyle",  rebarStyle },

                { "RebarBarType",rebarBarType},
                { "RebarBarTypeName",rebarBartypeName},

                { "StartRebarHookType",startRebarHookType},
                { "StartRebarHookTypeName",startRebarHookTypeName},

                { "EndRebarHookType",endRebarHookType},
                { "EndRebarHookTypeName",endRebarHookTypeName},

                { "RebarHostElement",rebarHostElement },

                { "Normal",normal},
                { "CurveList",curveList},

                { "StartRebarHookOrientation",startRebarHookOrientation},
                { "EndRebarHookOrientation",endRebarHookOrientation},

                { "MajorSegmentIndex",majorSegmentIndex},
                { "FreeFormOrShapeDriven",freeFormOrShapeDriven},
            };
            return rebarPropertyeDict;
        }


        //https://forums.autodesk.com/t5/revit-api-forum/revit-2022-api-checking-current-document-unit/td-p/10492609
        /// <summary>
        /// Get type properties of the selected rebar element
        /// </summary>
        /// <param name="dynamoRebar">Select Rebar instance in Revit</param>
        /// <returns>Offset</returns>
        [MultiReturn(new[] { "BarModelDiameter", "StandardBendDiameter", "StandardHookBendDiameter", "StirrupTieBendDiameter" })]
        public static Dictionary<string, object> GetRebarTypeProperties_ZTN(global::Revit.Elements.Element dynamoRebar)
        {
            Autodesk.Revit.DB.Document doc = DocumentManager.Instance.CurrentDBDocument;

            //get RebarBarType from selected Rebar
            //https://forums.autodesk.com/t5/revit-api-forum/get-property-value-of-rebar-bar-diameter-revit-api/td-p/5748929
            Rebar revitRebar = (Rebar)dynamoRebar.InternalElement;
            Autodesk.Revit.DB.Structure.RebarBarType revitBarType = (RebarBarType)doc.GetElement(revitRebar.GetTypeId());

            //Autodesk.Revit.DB.Structure.RebarBarType revitBarType = (Autodesk.Revit.DB.Structure.RebarBarType)barType.InternalElement;
            var getDocUnits = doc.GetUnits();
            //var getDisplayUnits = getDocUnits.GetFormatOptions(UnitType.UT_Length).DisplayUnits;
            var getDisplayUnits = getDocUnits.GetFormatOptions(Autodesk.Revit.DB.SpecTypeId.Length).GetUnitTypeId();

            //https://www.revitapidocs.com/2023/60c6aac3-8306-c56e-b62f-b7011b9ad7b6.htm
            return new Dictionary<string, object>
            {
                { "BarModelDiameter", Autodesk.Revit.DB.UnitUtils.ConvertFromInternalUnits(revitBarType.BarModelDiameter, getDisplayUnits) },
                { "StandardBendDiameter", Autodesk.Revit.DB.UnitUtils.ConvertFromInternalUnits(revitBarType.StandardBendDiameter, getDisplayUnits) },
                { "StandardHookBendDiameter", Autodesk.Revit.DB.UnitUtils.ConvertFromInternalUnits(revitBarType.StandardHookBendDiameter, getDisplayUnits) },
                { "StirrupTieBendDiameter", Autodesk.Revit.DB.UnitUtils.ConvertFromInternalUnits(revitBarType.StirrupTieBendDiameter, getDisplayUnits) },

            };
        }


        /// <summary>
        /// get all instances of Rebar Shape, which defines the shape of a rebar
        /// </summary>
        /// <param name="doc">Get application and document objects</param>
        /// <returns>true if some rebar shapes can be gotton, otherwise false</returns>
        private bool GetRebarShapes(Document doc)
        {
            // Initialize the _rebarShapes which used to store all rebar shapes.
            // Get all rebar shapes in revit and add them in _rebarShapes
            FilteredElementCollector filteredElementCollector = new FilteredElementCollector(doc);
            filteredElementCollector.OfClass(typeof(RebarShape));
            m_rebarShapes = filteredElementCollector.Cast<RebarShape>().ToList<RebarShape>();

            // If no rebar types in revit return false, otherwise true
            return 0 != m_rebarShapes.Count;
        }

        /// <summary>
        /// get all instances of Rebar Shape's name based on the rebar shape
        /// </summary>
        /// <param name="rebarShape">instance of a Rebar Shape, which defines the shape of a rebar</param>
        /// <returns>rebar shape's name</returns>
        public static string GetRebarShapeName(RebarShape rebarShape)
        {
            return rebarShape.Name;
        }

        /*

        //https://revitaddons.blogspot.com/2019/01/using-revit-api-to-retrieve-rebars.html
        /// <summary>
        /// A wrap fuction which used to create the reinforcement.
        /// </summary>
        /// <param name="dynamoRebar">select rebar element in Revit</param>
        /// <returns name="rebarHost">The host element of the rebar</returns> 
        /// <returns></returns>
        public static double RebarHostData_ZTN(Revit.Elements.Element dynamoRebar)
        {
            Autodesk.Revit.DB.Structure.Rebar rebar = (Rebar)dynamoRebar.InternalElement;
            // Compute the host face cover distance.

            if (RebarHostData.GetRebarHostData(rebar) != null)
            { 
                RebarHostData rebarHost = RebarHostData.GetRebarHostData(rebar);

                bool validObject = rebarHost.IsValidObject;
                // Get the Cover Distance
                double rebarCoverDistance = rebarHost.GetCommonCoverType().CoverDistance;
                return true;
            }
            return false;
        }
        */


        //https://forums.autodesk.com/t5/revit-api-forum/rebar-host-face-numbers/td-p/9254331
        //https://www.revitapidocs.com/2019/d9848d7d-5917-2433-8454-f65f5ac03964.htm
        /// <summary>
        /// A wrap fuction which used to retrieve host element of the selected rebar.
        /// </summary>
        /// <param name="dynamoRebar">select rebar element in Revit</param>
        /// <returns name="Rebar Host Element">The host element of the rebar</returns> 
        /// <returns></returns>
        public static global::Revit.Elements.Element GetRebarHost_ZTN(global::Revit.Elements.Element dynamoRebar)
        {
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            ////UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.Structure.Rebar rebar = (Rebar)dynamoRebar.InternalElement;
            ElementId host = rebar.GetHostId();
            //FamilyInstance elementInstance = (FamilyInstance)dynamoDocument.GetElement(host);
            global::Revit.Elements.Element rebarHost = dynamoDocument.GetElement(host).ToDSType(false);
            return rebarHost;
        }


        //https://www.revitapidocs.com/2017/7be7e413-bfac-bbd3-17b6-ff2008771afa.htm
        //https://forums.autodesk.com/t5/revit-api-forum/getcenterlinecurves-for-stirrup-rebar/td-p/7992243
        /// <summary>
        /// Get A chain of curves representing the centerline of the selected rebar element
        /// </summary>
        /// <param name="dynamoRebar">Select Rebar element in Revit</param>
        public static List<Autodesk.DesignScript.Geometry.Curve> GetRebarCentralLineCurves_ZTN(global::Revit.Elements.Element dynamoRebar)
        {
            ////UnWrap: get Revit element from the Dynamo-wrapped object
            Rebar revitRebar = (Rebar)dynamoRebar.InternalElement;

            //List<Autodesk.Revit.DB.Curve> curves = new List<Autodesk.Revit.DB.Curve>();
            List<Autodesk.DesignScript.Geometry.Curve> curveList = new List<Autodesk.DesignScript.Geometry.Curve>();

            //https://www.revitapidocs.com/2023/70fd7426-f4a4-591c-8c06-3c18dda45e7d.htm
            List<Autodesk.Revit.DB.Curve> curves = revitRebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0).ToList();
            // MultiplanarOption.IncludeOnlyPlanarCurves

            // get the center line curves of the rebar elements
            foreach (Autodesk.Revit.DB.Curve curve in curves)
            {
                // Get a DesignScript Curve from the Revit curve
                Autodesk.DesignScript.Geometry.Curve geocurve = curve.ToProtoType();
                curveList.Add(geocurve);
            }
            return curveList;
        }


        // https://gist.github.com/eibre/33cb25249c28b1c94a59b7ec6ca911a9
        /// <summary>
        /// Get rebar shape name in Revit API
        /// </summary>
        /// <returns name="rebarShapName">corresponding rebar shape name</returns> 
        /// <returns name="rebarShape">corresponding rebar shape</returns> 
        [MultiReturn(new[] { "rebarShapeName", "rebarShape" })]
        public static object GetRebarShape_ZTN(global::Revit.Elements.Element dynamoRebar)
        {

            //Get application and document objects
            Document doc = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            Autodesk.Revit.DB.Structure.Rebar rebar = (Rebar)dynamoRebar.InternalElement;

            Autodesk.Revit.DB.Parameter rebarShapeParameter = rebar.get_Parameter(BuiltInParameter.REBAR_SHAPE);
            Autodesk.Revit.DB.Element rebarShape = doc.GetElement(rebarShapeParameter.AsElementId());
            string rebarShapeName = rebarShape.Name;

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, object> rebarShapeDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                //{ "specTypeIdProperty",specTypeIdPropertyList},
                { "rebarShapeName",rebarShapeName},
                { "rebarShape",rebarShape},

            };
            return rebarShapeDict;

        }


        //https://www.revitapidocs.com/2020.1/cf39a5b8-d7f3-d073-0120-358dd3afab21.htm
        //https://www.revitapidocs.com/2020.1/b36327e1-c6be-791c-24a5-cf6d02890dee.htm
        /// <summary>
        /// Returns true if the rebar is free form and false if shape driven.
        /// </summary>
        /// <param name="dynamoRebar">Select Rebar element in Revit</param>
        /// <returns name="Free Form">Returns true if the rebar is free form and false if shape driven.</returns> 
        public static bool FreeFormOrShapeDriven_ZTN(global::Revit.Elements.Element dynamoRebar)
        {
            Autodesk.Revit.DB.Structure.Rebar rebar = (Rebar)dynamoRebar.InternalElement;
            return rebar.IsRebarFreeForm();
        }

        #endregion



        #region "set rebar layout"
        /// <summary>
        /// Sets the Layout Rule property of rebar set to SetLayoutAsMaximumSpacing
        /// </summary>
        /// <param name="dynamoRebar"> selected Rebar element to be replicated </param>
        /// <param name="spacing"> The maximum spacing between rebar in rebar set </param>
        /// <param name="arrayLength"> The distribution length of rebar set </param>
        /// <param name="barsOnNormalSide"> Identifies if the bars of the rebar set are on the same side of the rebar plane indicated by the normal </param>
        /// <param name="includeFirstBar"> Identifies if the first bar in rebar set is shown </param>
        /// <param name="includeLastBar"> Identifies if the last bar in rebar set is shown </param>
        /// <returns name="Rebar Set"> the new created rebar layout.</returns> 
        public static global::Revit.Elements.Element SetLayoutAsMaximumSpacing(global::Revit.Elements.Element dynamoRebar, double spacing, double arrayLength,
            bool barsOnNormalSide = true, bool includeFirstBar = true, bool includeLastBar = true)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.Structure.Rebar rebar = (Rebar)dynamoRebar.InternalElement;

            Autodesk.Revit.DB.Units getDocUnits = dynamoDocument.GetUnits();
            //https://www.revitapidocs.com/2023/32e858f2-d143-fe2c-76a5-38485382fb95.htm
            ForgeTypeId getDisplayUnits = getDocUnits.GetFormatOptions(Autodesk.Revit.DB.SpecTypeId.Length).GetUnitTypeId();

            //https://www.revitapidocs.com/2023/e70d4936-ecf2-dfbf-8caf-aac5d6a78d36.htm
            //Converts a value from a given unit to Revit's internal units.
            //Converts a given value to Revit's internal value by changing the Units
            //Tip: The Revit API uses decimal feet as its internal units system. This cannot be changed,so if you prefer working
            //in other unit systems(i.e.metric) or if your inputs are not decimal feet, you will have to perform the units
            //conversion within your code. It is also worth mentioning for angles, it uses radians, not degrees.
            double updatedSpacing = Autodesk.Revit.DB.UnitUtils.ConvertToInternalUnits(spacing, getDisplayUnits);
            double updatedArrayLength = Autodesk.Revit.DB.UnitUtils.ConvertToInternalUnits(arrayLength, getDisplayUnits);

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            if (rebar != null)
            {
                //https://www.revitapidocs.com/2023/c77085bd-db18-4869-bb2a-1e5c702e273a.htm
                //Returns an interface providing access to shape-driven properties and methods for this Rebar element.
                RebarShapeDrivenAccessor rebarShapeDrivenAccessor = rebar.GetShapeDrivenAccessor();
                if (rebarShapeDrivenAccessor != null)
                {
                    //https://www.revitapidocs.com/2023/fcadb398-7b67-9259-a1a2-c6afd831dc14.htm
                    //Sets the Layout Rule property of rebar set to MaximumSpacing  //updatedArrayLength
                    rebarShapeDrivenAccessor.SetLayoutAsMaximumSpacing(updatedSpacing, updatedArrayLength, barsOnNormalSide, includeFirstBar, includeLastBar);
                }
            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return dynamoRebar;
        }



        /// <summary>
        /// Sets the Layout Rule property of rebar set to MinimumClearSpacing
        /// </summary>
        /// <param name="dynamoRebar"> selected Rebar element to be replicated </param>
        /// <param name="spacing"> The maximum spacing between rebar in rebar set </param>
        /// <param name="arrayLength"> The distribution length of rebar set </param>
        /// <param name="barsOnNormalSide"> Identifies if the bars of the rebar set are on the same side of the rebar plane indicated by the normal </param>
        /// <param name="includeFirstBar"> Identifies if the first bar in rebar set is shown </param>
        /// <param name="includeLastBar"> Identifies if the last bar in rebar set is shown </param>
        /// <returns name="Rebar Set"> the new created rebar layout.</returns> 
        public static global::Revit.Elements.Element SetLayoutAsMinimumClearSpacing(global::Revit.Elements.Element dynamoRebar, double spacing, double arrayLength,
            bool barsOnNormalSide = true, bool includeFirstBar = true, bool includeLastBar = true)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.Structure.Rebar rebar = (Rebar)dynamoRebar.InternalElement;

            Autodesk.Revit.DB.Units getDocUnits = dynamoDocument.GetUnits();
            //https://www.revitapidocs.com/2023/32e858f2-d143-fe2c-76a5-38485382fb95.htm
            ForgeTypeId getDisplayUnits = getDocUnits.GetFormatOptions(Autodesk.Revit.DB.SpecTypeId.Length).GetUnitTypeId();

            //https://www.revitapidocs.com/2023/e70d4936-ecf2-dfbf-8caf-aac5d6a78d36.htm
            //Converts a value from a given unit to Revit's internal units.
            //Converts a given value to Revit's internal value by changing the Units
            //Tip: The Revit API uses decimal feet as its internal units system. This cannot be changed,so if you prefer working
            //in other unit systems(i.e.metric) or if your inputs are not decimal feet, you will have to perform the units
            //conversion within your code. It is also worth mentioning for angles, it uses radians, not degrees.
            double updatedSpacing = Autodesk.Revit.DB.UnitUtils.ConvertToInternalUnits(spacing, getDisplayUnits);
            double updatedArrayLength = Autodesk.Revit.DB.UnitUtils.ConvertToInternalUnits(arrayLength, getDisplayUnits);

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            if (rebar != null)
            {
                //https://www.revitapidocs.com/2023/c77085bd-db18-4869-bb2a-1e5c702e273a.htm
                //Returns an interface providing access to shape-driven properties and methods for this Rebar element.
                RebarShapeDrivenAccessor rebarShapeDrivenAccessor = rebar.GetShapeDrivenAccessor();
                if (rebarShapeDrivenAccessor != null)
                {
                    //https://www.revitapidocs.com/2023/fafd15e8-dc6b-7cc3-b6ec-c4ce9eb4b1af.htm
                    //Sets the Layout Rule property of rebar set to MinimumClearSpacing
                    rebarShapeDrivenAccessor.SetLayoutAsMinimumClearSpacing(updatedSpacing, updatedArrayLength, barsOnNormalSide, includeFirstBar, includeLastBar);
                }
            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return dynamoRebar;
        }



        /// <summary>
        /// Sets the Layout Rule property of rebar set to FixedNumber.
        /// </summary>
        /// <param name="dynamoRebar"> selected Rebar element to be replicated </param>
        /// <param name="numberOfBarPositions"> The number of bar positions in rebar set </param>
        /// <param name="arrayLength"> The distribution length of rebar set </param>
        /// <param name="barsOnNormalSide"> Identifies if the bars of the rebar set are on the same side of the rebar plane indicated by the normal </param>
        /// <param name="includeFirstBar"> Identifies if the first bar in rebar set is shown </param>
        /// <param name="includeLastBar"> Identifies if the last bar in rebar set is shown </param>
        /// <returns name="Rebar Set"> the new created rebar layout.</returns> 
        public static global::Revit.Elements.Element SetLayoutAsFixedNumber(global::Revit.Elements.Element dynamoRebar, int numberOfBarPositions, double arrayLength,
            bool barsOnNormalSide = true, bool includeFirstBar = true, bool includeLastBar = true)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.Structure.Rebar rebar = (Rebar)dynamoRebar.InternalElement;

            Autodesk.Revit.DB.Units getDocUnits = dynamoDocument.GetUnits();
            //https://www.revitapidocs.com/2023/32e858f2-d143-fe2c-76a5-38485382fb95.htm
            ForgeTypeId getDisplayUnits = getDocUnits.GetFormatOptions(Autodesk.Revit.DB.SpecTypeId.Length).GetUnitTypeId();

            //https://www.revitapidocs.com/2023/e70d4936-ecf2-dfbf-8caf-aac5d6a78d36.htm
            //Converts a value from a given unit to Revit's internal units.
            //Converts a given value to Revit's internal value by changing the Units
            //Tip: The Revit API uses decimal feet as its internal units system. This cannot be changed,so if you prefer working
            //in other unit systems(i.e.metric) or if your inputs are not decimal feet, you will have to perform the units
            //conversion within your code. It is also worth mentioning for angles, it uses radians, not degrees.
            double updatedArrayLength = Autodesk.Revit.DB.UnitUtils.ConvertToInternalUnits(arrayLength, getDisplayUnits);

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            if (rebar != null)
            {
                //https://www.revitapidocs.com/2023/c77085bd-db18-4869-bb2a-1e5c702e273a.htm
                //Returns an interface providing access to shape-driven properties and methods for this Rebar element.
                RebarShapeDrivenAccessor rebarShapeDrivenAccessor = rebar.GetShapeDrivenAccessor();
                if (rebarShapeDrivenAccessor != null)
                {
                    //https://www.revitapidocs.com/2023/017a567e-6087-745c-ed82-4a71b42ea203.htm
                    //Sets the Layout Rule property of rebar set to FixedNumber.
                    rebarShapeDrivenAccessor.SetLayoutAsFixedNumber(numberOfBarPositions, updatedArrayLength, barsOnNormalSide, includeFirstBar, includeLastBar);
                }
            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return dynamoRebar;
        }

        /// <summary>
        /// Sets the Layout Rule property of rebar set to NumberWithSpacing
        /// </summary>
        /// <param name="dynamoRebar"> selected Rebar element to be replicated </param>
        /// <param name="numberOfBarPositions"> The number of bar positions in rebar set </param>
        /// <param name="spacing"> The maximum spacing between rebar in rebar set </param>
        /// <param name="barsOnNormalSide"> Identifies if the bars of the rebar set are on the same side of the rebar plane indicated by the normal </param>
        /// <param name="includeFirstBar"> Identifies if the first bar in rebar set is shown </param>
        /// <param name="includeLastBar"> Identifies if the last bar in rebar set is shown </param>
        /// <returns name="Rebar Set"> the new created rebar layout.</returns> 
        public static global::Revit.Elements.Element SetLayoutAsNumberWithSpacing(global::Revit.Elements.Element dynamoRebar, int numberOfBarPositions, double spacing,
            bool barsOnNormalSide = true, bool includeFirstBar = true, bool includeLastBar = true)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.Structure.Rebar rebar = (Rebar)dynamoRebar.InternalElement;

            Autodesk.Revit.DB.Units getDocUnits = dynamoDocument.GetUnits();
            //https://www.revitapidocs.com/2023/32e858f2-d143-fe2c-76a5-38485382fb95.htm
            ForgeTypeId getDisplayUnits = getDocUnits.GetFormatOptions(Autodesk.Revit.DB.SpecTypeId.Length).GetUnitTypeId();

            //https://www.revitapidocs.com/2023/e70d4936-ecf2-dfbf-8caf-aac5d6a78d36.htm
            //Converts a value from a given unit to Revit's internal units.
            //Converts a given value to Revit's internal value by changing the Units
            //Tip: The Revit API uses decimal feet as its internal units system. This cannot be changed,so if you prefer working
            //in other unit systems(i.e.metric) or if your inputs are not decimal feet, you will have to perform the units
            //conversion within your code. It is also worth mentioning for angles, it uses radians, not degrees.
            double updatedSpacing = Autodesk.Revit.DB.UnitUtils.ConvertToInternalUnits(spacing, getDisplayUnits);

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            if (rebar != null)
            {
                //https://www.revitapidocs.com/2023/c77085bd-db18-4869-bb2a-1e5c702e273a.htm
                //Returns an interface providing access to shape-driven properties and methods for this Rebar element.
                RebarShapeDrivenAccessor rebarShapeDrivenAccessor = rebar.GetShapeDrivenAccessor();
                if (rebarShapeDrivenAccessor != null)
                {
                    //https://www.revitapidocs.com/2023/3d8b7f68-cfe0-c1ac-c8b3-532a80155e0d.htm
                    //Sets the Layout Rule property of rebar set to NumberWithSpacing
                    rebarShapeDrivenAccessor.SetLayoutAsNumberWithSpacing(numberOfBarPositions, updatedSpacing, barsOnNormalSide, includeFirstBar, includeLastBar);
                }
            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return dynamoRebar;
        }

        /// <summary>
        /// Sets the Layout Rule property of rebar set to Single
        /// </summary>
        /// <param name="dynamoRebar"> selected Rebar element to be replicated </param>
        /// <returns name="Rebar Set"> the new created rebar layout.</returns> 
        public static global::Revit.Elements.Element SetLayoutAsSingle(global::Revit.Elements.Element dynamoRebar)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.Structure.Rebar rebar = (Rebar)dynamoRebar.InternalElement;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            if (rebar != null)
            {
                //https://www.revitapidocs.com/2023/c77085bd-db18-4869-bb2a-1e5c702e273a.htm
                //Returns an interface providing access to shape-driven properties and methods for this Rebar element.
                RebarShapeDrivenAccessor rebarShapeDrivenAccessor = rebar.GetShapeDrivenAccessor();
                if (rebarShapeDrivenAccessor != null)
                {
                    //https://www.revitapidocs.com/2023/d9e95eca-6e25-eb15-3ee9-49e61f9b5e2b.htm
                    //Sets the Layout Rule property of rebar set to Single.
                    rebarShapeDrivenAccessor.SetLayoutAsSingle();
                }
            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return dynamoRebar;
        }

        /// <summary>
        /// The helper function to changed rebar number and spacing properties
        /// </summary>
        /// <param name="bar">The rebar instance which need to modify</param>
        /// <param name="number">The rebar number want to set</param>
        /// <param name="spacing">The spacing want to set</param>
        protected static void SetRebarSpaceAndNumber(Rebar bar, int number, double spacing)
        {
            // Asset the parameter is valid
            if (null == bar || 2 > number || 0 > spacing)
            {
                return;
            }

            //https://www.revitapidocs.com/2024/3d8b7f68-cfe0-c1ac-c8b3-532a80155e0d.htm
            // Change the rebar number and spacing properties
            bar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(number, spacing, true, true, true);
        }
        #endregion


        #region "rebar visibility control"
        /// <summary>
        /// Select all views in the Revit model.
        /// </summary>
        /// <returns name="Views"> retrun views in the model.</returns> 
        public static List<global::Revit.Elements.Element> CollectAllViews()
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            // Use ElementClassFilter to find all loads in the document
            // Using typeof(LoadBase) will yield all AreaLoad, LineLoad and PointLoad
            ElementClassFilter filter = new ElementClassFilter(typeof(Autodesk.Revit.DB.View));

            // Apply the filter to the elements in the active document
            FilteredElementCollector collector = new FilteredElementCollector(dynamoDocument);
            collector.WherePasses(filter);
            ICollection<Autodesk.Revit.DB.Element> allRevitViews = collector.ToElements();
            List<global::Revit.Elements.Element> allDynamoViews = new List<global::Revit.Elements.Element>();

            foreach (Autodesk.Revit.DB.Element revitView in allRevitViews)
            {
                global::Revit.Elements.Element viewDynamo = revitView.ToDSType(false);
                allDynamoViews.Add(viewDynamo);
            }

            return allDynamoViews;
        }

        /// <summary>
        /// retrun view according to the provided viewName.
        /// </summary>
        /// <param name="viewName"> view name in string </param>
        /// <returns name="Views"> retrun view according to the provided viewName.</returns> 
        public static global::Revit.Elements.Element GetViewAccordingToName(string viewName)
        {
            //collect all views and save as a list
            List<global::Revit.Elements.Element> allDynamoViewList = StructuralRebar.CollectAllViews();

            global::Revit.Elements.Element element = null;

            foreach (global::Revit.Elements.Element dynamoView in allDynamoViewList)
            {
                if (dynamoView != null)
                {
                    //UnWrap: get Revit element from the Dynamo-wrapped object
                    Autodesk.Revit.DB.View revitView = (Autodesk.Revit.DB.View)dynamoView.InternalElement;
                    if (revitView.Name == viewName)
                    {
                        //boolList.Add(true);
                        element = dynamoView;
                        break;
                    }
                }
            }
            return element;
        }

        /// <summary>
        /// Edit rebar visibility setting at the view.
        /// </summary>
        /// <param name="dynamoRebar"> the selected rebar to Edit visibility </param>
        /// <param name="dynamoView"> make the rebar visible at this view </param>
        /// <returns name="Rebar"> retrun view according to the provided viewName.</returns> 
        public static global::Revit.Elements.Element ShowRebarInView(global::Revit.Elements.Element dynamoRebar, global::Revit.Elements.Views.View dynamoView)
        {
            Autodesk.Revit.DB.Structure.Rebar rebar = (Autodesk.Revit.DB.Structure.Rebar)dynamoRebar.InternalElement;
            Autodesk.Revit.DB.View revitView = (Autodesk.Revit.DB.View)dynamoView.InternalElement;

            rebar.SetUnobscuredInView(revitView, true);

            if (revitView.ViewType == ViewType.ThreeD)
            {
                //https://www.revitapidocs.com/2015/ab857136-3b6f-5c0e-b28c-5ea5f7c3be79.htm
                //Sets this rebar element to be shown unobscured in a view.
                //rebar.IsUnobscuredInView(revitView);
                rebar.SetUnobscuredInView(revitView, true);
                //https://www.revitapidocs.com/2023/f030d783-7390-38dc-a83a-b1afaa895162.htm
                //[ObsoleteAttribute("This method is deprecated in Revit 2023 and may be removed in a later version of Revit.
                //The Rebar will always be shown solidly in 3D views with Fine level of detail. To change this, you can
                //override the detail level of view for Structural Rebar category.")]
                rebar.SetSolidInView((Autodesk.Revit.DB.View3D)revitView, true);
            }
            else
            {
                //https://www.revitapidocs.com/2015/ab857136-3b6f-5c0e-b28c-5ea5f7c3be79.htm
                //Sets this rebar element to be shown unobscured in a view.
                rebar.SetUnobscuredInView(revitView, true);
            }

            return dynamoRebar;
        }

        /// <summary>
        /// Show the given rebar as solid in 3d view.
        /// </summary>
        /// <param name="rebar">Rebar to show in 3d view as solid</param>
        private static void ShowRebar3d(Rebar rebar)
        {
            var filter = new FilteredElementCollector(rebar.Document).OfClass(typeof(Autodesk.Revit.DB.View3D));

            //foreach (View3D view in filter)
            foreach (Autodesk.Revit.DB.View3D view in filter.Cast<Autodesk.Revit.DB.View3D>())
            {
                //https://www.revitapidocs.com/2015/ab857136-3b6f-5c0e-b28c-5ea5f7c3be79.htm
                //Sets this rebar element to be shown unobscured in a view.
                rebar.SetUnobscuredInView(view, true);
                //https://www.revitapidocs.com/2023/f030d783-7390-38dc-a83a-b1afaa895162.htm
                //[ObsoleteAttribute("This method is deprecated in Revit 2023 and may be removed in a later version of Revit.
                //The Rebar will always be shown solidly in 3D views with Fine level of detail. To change this, you can
                //override the detail level of view for Structural Rebar category.")]
                rebar.SetSolidInView(view, true);
            }
        }

        #endregion


        #region "rebar property in current model"
        /// <summary>
        /// get all the hook types in current project, and store in m_rebarHookTypes data
        /// </summary>
        /// <param name="doc">Get application and document objects</param>
        /// <returns>true if some hook types can be gotton, otherwise false</returns>
        private bool GetHookTypes(Document doc)
        {
            // Initialize the m_rebarHookTypes which used to store all hook types.
            FilteredElementCollector filteredElementCollector = new FilteredElementCollector(doc);
            filteredElementCollector.OfClass(typeof(RebarHookType));
            m_rebarHookTypes = filteredElementCollector.Cast<RebarHookType>().ToList<RebarHookType>();

            // If no hook types in revit return false, otherwise true
            //return (0 == m_rebarHookTypes.Count) ? false : true;
            return 0 != m_rebarHookTypes.Count;
        }

        /// <summary>
        /// get all the rebar types in current project, and store in m_rebarBarTypes data
        /// </summary>
        /// <param name="doc">Get application and document objects</param>
        /// <returns>true if some rebar types can be gotton, otherwise false</returns>
        private bool GetRebarTypes(Document doc)
        {
            // Initialize the m_rebarBarTypes which used to store all rebar types.
            // Get all rebar types in revit and add them in m_rebarBarTypes
            FilteredElementCollector filteredElementCollector = new FilteredElementCollector(doc);
            filteredElementCollector.OfClass(typeof(RebarBarType));
            m_rebarBarTypes = filteredElementCollector.Cast<RebarBarType>().ToList<RebarBarType>();

            // If no rebar types in revit return false, otherwise true
            //return (0 == m_rebarBarTypes.Count) ? false : true;
            return 0 != m_rebarBarTypes.Count;
        }


        //https://www.revitapidocs.com/2023/a9ac65a6-29e6-25e5-caca-502e21385f47.htm
        /// <summary>
        /// get all the rebar style in current project, and store in m_rebarStyles data
        /// </summary>
        /// <returns>true if some rebar styles can be gotton, otherwise false</returns>
        private bool GetRebarStyle_ZTN()
        {
            m_rebarStyles = Enum.GetValues(typeof(RebarStyle)).Cast<RebarStyle>().ToList();

            // If no rebar types in revit return false, otherwise true
            return 0 != m_rebarStyles.Count;
        }

        //https://www.revitapidocs.com/2023/e8365754-0811-8d4e-864a-55bf34af3a87.htm
        /// <summary>
        /// get all the rebar Hook Orientation in current project, and store in m_rebarHookOrientations data
        /// </summary>
        /// <returns>true if some rebar styles can be gotton, otherwise false</returns>
        private bool GetRebarHookOrientation_ZTN()
        {
            m_rebarHookOrientations = Enum.GetValues(typeof(RebarHookOrientation)).Cast<RebarHookOrientation>().ToList();

            // If no rebar types in revit return false, otherwise true
            return 0 != m_rebarStyles.Count;
        }
        #endregion


        #region "no category"


        /// <summary>
        /// Override rebar Hook Lengths (family instance parameter), 0 means uncheck, 1 means check
        /// </summary>
        /// <param name="dynamoRebar"> select structural rebar in Revit </param>
        /// <param name="newValue"> Override rebar Hook Lengths option, 0 means uncheck, 1 means check </param>
        /// <returns name="Rebar"> updated Rebar with new parameter value.</returns> 
        public static global::Revit.Elements.Element OverrideHookLengths(global::Revit.Elements.Element dynamoRebar, int newValue = 1)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            // Get the element you want to modify
            Autodesk.Revit.DB.Structure.Rebar rebar = (Rebar)dynamoRebar.InternalElement;

            // Get the parameter you want to modify
            Autodesk.Revit.DB.Parameter rebarHookLengthsParameter = rebar.get_Parameter(BuiltInParameter.REBAR_HOOK_LENGTH_OVERRIDE);
            //Parameter parameter = rebar.LookupParameter("OverrideHookLengths"); 
            //https://www.revitapidocs.com/2024/c0343d88-ea6f-f718-2828-7970c15e4a9e.htm

            //// Check if the parameter is writable
            //if (parameter.IsReadOnly)
            //{
            //    // Parameter is read-only, cannot be modified
            //    return;
            //}
            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            // Set the parameter value
            rebarHookLengthsParameter.Set(newValue);

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return dynamoRebar;
        }




        #endregion

    }
}

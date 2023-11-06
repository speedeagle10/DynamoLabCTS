using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.DesignScript.Geometry;
using Dynamo.Graph.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Revit.GeometryConversion;

namespace DynamoLab.Revit.Elements
{
    /// <summary>
    /// Utility class that contains methods of StructuralFraming: Column, Beam, Bracing.
    /// </summary>
    public class StructuralFraming
    {
        private StructuralFraming() { }


        //https://www.revitapidocs.com/2015/400cc9b6-9ff7-de85-6fd8-c20002209d25.htm
        /// <summary>
        /// get the Structural framing _ column centrial line, this method only works for Revit 2022 and former versions. After the retire of 
        /// GetAnalyticalModel from Revit 2023, this method does not work anymore
        /// </summary>
        /// <param name="dynamoColumn"> select structural framing _ column in Revit </param>
        /// <returns name="Curve"> the curve of the column.</returns> 
        [NodeCategory("Query")]  //works because we addusing Dynamo.Graph.Nodes; Reference from DynamoVisualProgramming.Core at NuGet Package
        public static Autodesk.DesignScript.Geometry.Curve GetLocationCurve2022(global::Revit.Elements.Element dynamoColumn)
        {
            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.FamilyInstance column = (Autodesk.Revit.DB.FamilyInstance)dynamoColumn.InternalElement;

            AnalyticalModel modelColumn = column.GetAnalyticalModel();
            Autodesk.Revit.DB.Curve columnCurve =null;
            // column should be represented by a single curve
            if (modelColumn.IsSingleCurve() == true)
            {
                columnCurve = modelColumn.GetCurve();
            }

            Autodesk.DesignScript.Geometry.Curve dynamoCurve = columnCurve.ToProtoType();

            return dynamoCurve;
        }

        /*
        //https://thebuildingcoder.typepad.com/blog/2022/04/tbc-samples-2023-and-the-new-structural-api.html
        //https://forums.autodesk.com/t5/revit-api-forum/analyticalmodel-not-accessible-in-revit-2023-api/td-p/11101063
        //https://help.autodesk.com/view/RVT/2023/ENU/?guid=GUID-A1157199-4E27-41F9-BF45-53A5CD79E9A1
        /// <summary>
        /// get the Structural framing _ column centrial line, this method only works for Revit 2023 and later versions. After the retire of 
        /// GetAnalyticalModel from Revit 2023, this method works
        /// </summary>
        /// <param name="dynamoColumn"> select structural framing _ column in Revit </param>
        /// <returns name="Curve"> the curve of the column.</returns> 
        [NodeCategory("Query")]  //works because we addusing Dynamo.Graph.Nodes; Reference from DynamoVisualProgramming.Core at NuGet Package
        public static Autodesk.DesignScript.Geometry.Curve GetLocationCurve2023(global::Revit.Elements.Element dynamoColumn)
        {
            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.Element column = (Autodesk.Revit.DB.Element)dynamoColumn.InternalElement;


            Autodesk.Revit.DB.Structure.AnalyticalElement modelColumn = GetAnalyticalElementId(column);


            //Autodesk.Revit.DB.Curve columnCurve = null;
            // column should be represented by a single curve

            //https://www.revitapidocs.com/2023/1eebc63e-4b20-a2a1-3537-283e2d284ee4.htm
            Autodesk.Revit.DB.Curve columnCurve = modelColumn.GetCurve();

            Autodesk.DesignScript.Geometry.Curve dynamoCurve = columnCurve.ToProtoType();

            return dynamoCurve;
        }


        //https://www.revitapidocs.com/2023/0f7f395b-3f70-aa6e-e584-b70c11f767ad.htm
        /// <summary>
        /// Return the associated analytical element id for the given element
        /// </summary>
        private static ElementId GetAnalyticalElementId(Element ele)
        {
            Document doc = ele.Document;

            Autodesk.Revit.DB.Structure.AnalyticalToPhysicalAssociationManager m
              = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(doc);

            if (null == m)
            {
                throw new System.ArgumentException(
                  "No AnalyticalToPhysicalAssociationManager found");
            }

            return m.GetAssociatedElementId(ele.Id);
        }
        */


        /// <summary>
        /// Structural framing _ column length
        /// </summary>
        /// <param name="dynamoColumn"> select structural framing _ column in Revit </param>
        /// <returns name="Column Length"> the length of the column.</returns> 
        [NodeCategory("Query")]  //works because we addusing Dynamo.Graph.Nodes; Reference from DynamoVisualProgramming.Core at NuGet Package
        public static double StructuralColumnLength(global::Revit.Elements.Element dynamoColumn)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Autodesk.Revit.DB.FamilyInstance column = (Autodesk.Revit.DB.FamilyInstance)dynamoColumn.InternalElement;


            Autodesk.Revit.DB.Parameter columnLengthParameter = column.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM);
            double columnLength = columnLengthParameter.AsDouble();

            Autodesk.Revit.DB.Units getDocUnits = dynamoDocument.GetUnits();
            //https://www.revitapidocs.com/2023/32e858f2-d143-fe2c-76a5-38485382fb95.htm
            ForgeTypeId getDisplayUnits = getDocUnits.GetFormatOptions(Autodesk.Revit.DB.SpecTypeId.Length).GetUnitTypeId();

            //https://www.revitapidocs.com/2023/e70d4936-ecf2-dfbf-8caf-aac5d6a78d36.htm
            //Converts a value from Revit's internal units to a given unit.
            double updatedColumnLength = Autodesk.Revit.DB.UnitUtils.ConvertFromInternalUnits(columnLength, getDisplayUnits);

            return updatedColumnLength;
        }
    }
}

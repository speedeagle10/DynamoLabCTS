using Autodesk.DesignScript.Runtime;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Dynamo.Graph.Nodes;
using Revit.GeometryConversion;
using RevitServices.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DynamoLab.Revit.Elements
{
    ////https://www.revitapidocs.com/2023/1d872359-d091-1666-b898-98727c7be03b.htm
    /// <summary>
    /// Class for RebarCoupler
    /// </summary>
    public class RebarCoupler
    {
        private RebarCoupler() { }

        //https://www.revitapidocs.com/2023/52fc10f6-e5b0-0f47-20fa-90be57b48004.htm
        /// <summary>
        /// Creates a new instance of a Rebar Coupler element within two Rebar.
        /// </summary>
        /// <param name="dynamoRebarList"> List contains 2 rebars elements or List contains 2 rebar sets select at the Revit model.</param>
        /// <returns name="RebarCoupler"> New created instance of a Rebar Coupler element within the project. </returns> 
        [NodeCategory("Create")]  //3 option Create / Actions / Query
        public static Autodesk.Revit.DB.Structure.RebarCoupler RebarCouplerBetween2Rebars(List<global::Revit.Elements.Element> dynamoRebarList)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            List<Rebar> revitRebarList = new List<Rebar>();
            foreach (global::Revit.Elements.Element dynamoRebar in dynamoRebarList)
            {
                Rebar revitRebar = (Rebar)dynamoRebar.InternalElement;
                revitRebarList.Add(revitRebar);
            }

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            Autodesk.Revit.DB.Structure.RebarCoupler rebarCoupler = null;
            // if we have at least 2 bars, create a rebarCoupler between them
            if (revitRebarList.Count > 1)
            {
                // get a type id for the Coupler
                ElementId rebarCouplerElementId = dynamoDocument.GetDefaultFamilyTypeId(new ElementId(BuiltInCategory.OST_Coupler));

                if (rebarCouplerElementId != ElementId.InvalidElementId)
                {
                    // Specify the rebar and ends to couple
                    //RebarReinforcementData rebarData1 = RebarReinforcementData.Create(revitRebarList[0].Id, 0);
                    //RebarReinforcementData rebarData2 = RebarReinforcementData.Create(revitRebarList[1].Id, 1);

                    //https://www.revitapidocs.com/2023/6186fbd9-2791-01b7-c45e-00480ff50d83.htm
                    //https://www.revitapidocs.com/2023/3be68918-5d12-01f9-c267-b44717592bd4.htm
                    ReinforcementData rebarData1 = (ReinforcementData)RebarReinforcementData.Create(revitRebarList[0].Id, 0);
                    ReinforcementData rebarData2 = (ReinforcementData)RebarReinforcementData.Create(revitRebarList[1].Id, 1);

                    rebarCoupler = Autodesk.Revit.DB.Structure.RebarCoupler.Create(dynamoDocument, rebarCouplerElementId, rebarData1, rebarData2, out RebarCouplerError error);
                    if (error != RebarCouplerError.ValidationSuccessfuly)
                    {
                        TaskDialog.Show("Revit", "Create Coupler failed: " + error.ToString());
                    }

                }
            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return rebarCoupler;
        }


        //https://www.revitapidocs.com/2023/52fc10f6-e5b0-0f47-20fa-90be57b48004.htm
        /// <summary>
        /// Use a coupler to cap the other end of the first bar.
        /// </summary>
        /// <param name="dynamoRebar"> rebar or rebar set select at the Revit model.</param>
        /// <returns name="RebarCoupler"> New created instance of a Rebar Coupler element at the end of the rebar. </returns> 
        [NodeCategory("Create")]  //3 option Create / Actions / Query
        public static Autodesk.Revit.DB.Structure.RebarCoupler RebarCouplerCap(global::Revit.Elements.Element dynamoRebar)
        {
            //Get application and document objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Rebar revitRebar = (Rebar)dynamoRebar.InternalElement;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            // get a type id for the Coupler
            ElementId rebarCouplerElementId = dynamoDocument.GetDefaultFamilyTypeId(new ElementId(BuiltInCategory.OST_Coupler));

            //https://www.revitapidocs.com/2022/e1df380e-37e8-0bd0-0291-fc4e8c38d747.htm
            // Creates a new instance of RebarReinforcementData. The end of rebar where the coupler stays. This should be 0 or 1
            ReinforcementData rebarData = (ReinforcementData)RebarReinforcementData.Create(revitRebar.Id, 1);

            // Use a coupler to cap the other end of the first bar.
            Autodesk.Revit.DB.Structure.RebarCoupler rebarCoupler = Autodesk.Revit.DB.Structure.RebarCoupler.Create(dynamoDocument, rebarCouplerElementId, rebarData, null, out RebarCouplerError error);
            if (error != RebarCouplerError.ValidationSuccessfuly)
            {
                TaskDialog.Show("Revit", "Create Coupler failed: " + error.ToString());
            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return rebarCoupler;
        }
    }
}

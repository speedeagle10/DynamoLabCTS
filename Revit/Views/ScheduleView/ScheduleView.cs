using Autodesk.DesignScript.Runtime;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitServices.Transactions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DynamoLabCTS.Revit.Views
{
    /// <summary>
    /// Utility class that contains methods of view schedule creation and schedule sheet instance creation.
    /// </summary>
    public class ScheduleView
    {
        /// <summary>
        /// Create a view schedule of ViewSchedule category and add schedule field, filter and sorting/grouping field to it.
        /// </summary>
        /// <param name="categoryName">Name of the Element Category.</param>
        /// <param name="scheduleName">Name of the New created schedule.</param>
        /// <param name="fieldNames">Name of the New created schedule.</param>
        /// <returns name="Schedule">new created schedule(s) with selected Field</returns> 
        public static ViewSchedule CreateSchedules_ZTN(string categoryName, string scheduleName, List<string> fieldNames)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories = document.Settings.Categories;
            //https://www.revitapidocs.com/2022/a8dc01fc-7c56-2fd8-8340-a7fbf7bcc7b4.htm
            Autodesk.Revit.DB.Category category = categories.get_Item(categoryName);
            BuiltInCategory builtInCategory = (BuiltInCategory)category.Id.IntegerValue;

            //Create an empty view schedule of viewSchedule category.
            ViewSchedule schedule = ViewSchedule.CreateSchedule(document, new ElementId(builtInCategory));
            document.Regenerate();
            schedule.Name = scheduleName;
            foreach (string fieldName in fieldNames)
            {
                AddRegularFieldToSchedule_ZTN(document, schedule, fieldName);
            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        /// <summary>
        /// map from Dynamo Category to BuildinCategory.
        /// </summary>
        /// <param name="category">Dynamo Category.</param>
        /// <returns name="BuiltInCategory">BuiltIn Category</returns> 
        /// <returns name="BuiltInCategory Name">BuiltIn Category name</returns>
        /// <returns name="Category Name">BuiltIn Category name</returns>
        [MultiReturn(new[] { "builtInCategory", "builtInCategoryName", "categoryName" })]
        public static Dictionary<string, object> Category2BuiltInCategory_ZTN(global::Revit.Elements.Category category)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories = document.Settings.Categories;
            Autodesk.Revit.DB.Category category4Schedule = categories.get_Item(category.Name);
            BuiltInCategory builtInCategory = (BuiltInCategory)category4Schedule.Id.IntegerValue;
            string builtInCategoryName = builtInCategory.ToString();

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            //https://www.revitapidocs.com/2023/a055809c-b743-d17f-aef2-31cfa208ecc1.htm
            Dictionary<string, object> categoryDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                { "builtInCategory", builtInCategory},
                { "builtInCategoryName", builtInCategoryName},
                { "categoryName", category.Name},
            };

            return categoryDict;
        }


        /// <summary>
        /// Create a view schedule of ViewSchedule category and add schedule field, filter and sorting/grouping field to it.
        /// </summary>
        /// <param name="category">Schedule Category.</param>
        /// <param name="scheduleName">Name of the New created schedule.</param>
        /// <param name="fieldNames">Name of the New created schedule.</param>
        /// <param name="includeElementsInLinks">Check the option Include Elements In Links.</param>
        /// <returns name="Schedule">new created schedule(s) with selected Field</returns> 
        public static ViewSchedule CreateSchedules_IncludeElementsInLinks_ZTN(global::Revit.Elements.Category category, string scheduleName, List<string> fieldNames, bool includeElementsInLinks = true)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories = document.Settings.Categories;
            Autodesk.Revit.DB.Category category4Schedule = categories.get_Item(category.Name);
            BuiltInCategory builtInCategory = (BuiltInCategory)category4Schedule.Id.IntegerValue;

            //Create an empty view schedule of viewSchedule category.
            ViewSchedule schedule = ViewSchedule.CreateSchedule(document, new ElementId(builtInCategory));
            document.Regenerate();
            schedule.Name = scheduleName;

            //Check the option Include Elements In Links to include information from linked models.
            schedule.Definition.IncludeLinkedFiles = includeElementsInLinks;

            foreach (string fieldName in fieldNames)
            {
                AddRegularFieldToSchedule_ZTN(document, schedule, fieldName);
            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        private static void AddRegularFieldToSchedule_ZTN(Document doc, ViewSchedule schedule, string fieldName)
        {
            ScheduleDefinition definition = schedule.Definition;

            // Find a matching SchedulableField
            Autodesk.Revit.DB.SchedulableField schedulableField =
                definition.GetSchedulableFields().FirstOrDefault<Autodesk.Revit.DB.SchedulableField>(sf => sf.GetName(doc) == fieldName);

            if (schedulableField != null)
            {
                // Add the found field
                definition.AddField(schedulableField);
            }

        }

        /// <summary>
        /// Change Schedule header to a new name.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="newHeaderName">Name of the New created schedule.</param>
        /// <returns name="Schedule">schedule(s) with new header</returns> 
        public static Autodesk.Revit.DB.ViewSchedule SetScheduleHeader_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule, string newHeaderName)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;


            // Refresh data to be sure that schedule is ready for Header Change
            schedule.RefreshData();

            //Get header section
            //https://www.revitapidocs.com/2018/1c7a694b-73c0-8d15-7817-5620b6e83098.htm
            TableSectionData tsd = schedule.GetTableData().GetSectionData(SectionType.Header);

            //Set text to the first header cell
            tsd.SetCellText(tsd.FirstRowNumber, tsd.FirstColumnNumber, newHeaderName);

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        // https://help.autodesk.com/view/RVT/2024/ENU/?guid=Revit_API_Revit_API_Developers_Guide_Basic_Interaction_with_Revit_Elements_Views_View_Types_TableView_ViewSchedule_Working_with_ViewSchedule_html

        /// <summary>
        /// Add filter to selected schedule.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="fieldName">Name of the scheduled field.</param>
        /// <param name="filterType">FilterType of the scheduled field.</param>
        /// <param name="filterInformation">filter information of the scheduled field.</param>
        /// <returns name="Schedule">schedule(s) with new header</returns> 
        public static Autodesk.Revit.DB.ViewSchedule AddFilter_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule,
            string fieldName, ScheduleFilterType filterType, string filterInformation)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            //Remove filter if needed
            //https://www.revitapidocs.com/2019/57b69056-4175-3de4-511f-c1a1af280300.htm

            IList<ScheduleFieldId> sourceFieldOrder = definition.GetFieldOrder();

            // Refresh data to be sure that schedule is ready for Header Change
            schedule.RefreshData();
            foreach (ScheduleFieldId fieldId in sourceFieldOrder)
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                //if (foundField.ColumnHeading == fieldName) This works if we had not change the column header
                if (foundField.GetName() == fieldName)
                {
                    //https://www.revitapidocs.com/2023/6ec07804-d396-ad9b-d0b8-08b37b3b9ae7.htm
                    Autodesk.Revit.DB.ScheduleFilter filter = new Autodesk.Revit.DB.ScheduleFilter(fieldId, filterType, filterInformation);
                    //ScheduleFilterType.Contains
                    definition.AddFilter(filter);
                }
            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        //https://help.autodesk.com/view/RVT/2024/ENU/?guid=Revit_API_Revit_API_Developers_Guide_Basic_Interaction_with_Revit_Elements_Views_View_Types_TableView_ViewSchedule_Working_with_ViewSchedule_html

        /// <summary>
        /// sort selected field at selected schedule.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="fieldName">Name of the scheduled field.</param>
        /// <param name="ascendingOrDescending">choose sort option: true means Ascending, false means Descending.</param>
        /// <returns name="Schedule">schedule(s) with new header</returns> 
        public static Autodesk.Revit.DB.ViewSchedule SortSchedule_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule, string fieldName, bool ascendingOrDescending = false)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            // clear the sort setting before assign new rules
            //https://www.revitapidocs.com/2019/645645fa-52b8-b747-e3d4-b5428962a4bc.htm
            // If you did not include this line, Revit will add new sort rules for the field and this may cause problem
            // in case users don't want to clear sort filed, we can remove this line or add conditional statement
            definition.ClearSortGroupFields();

            IList<ScheduleFieldId> sourceFieldOrder = definition.GetFieldOrder();

            // Refresh data to be sure that schedule is ready for Header Change
            schedule.RefreshData();
            foreach (ScheduleFieldId fieldId in sourceFieldOrder)
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                //if (foundField.ColumnHeading == fieldName) This works if we had not change the column header
                if (foundField.GetName() == fieldName)
                {
                    if (ascendingOrDescending == false)
                    {
                        // Build sort/group field.
                        ScheduleSortGroupField sortGroupField = new ScheduleSortGroupField(fieldId, ScheduleSortOrder.Descending);

                        // Add the sort/group field
                        definition.AddSortGroupField(sortGroupField);
                    }
                    else
                    {
                        // Build sort/group field.
                        ScheduleSortGroupField sortGroupField = new ScheduleSortGroupField(fieldId, ScheduleSortOrder.Ascending);

                        // Add the sort/group field
                        definition.AddSortGroupField(sortGroupField);
                    }
                }
            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        //https://www.revitapidocs.com/2015/3890f745-6f24-f81a-9f8f-d8b47c8e3f94.htm

        /// <summary>
        /// change field heading at selected schedule.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="fieldNameList">Name of the scheduled field.</param>
        /// <param name="newFieldNameList"> new Name of the scheduled field.</param>
        /// <returns name="Schedule">schedule(s) with new header</returns> 
        public static Autodesk.Revit.DB.ViewSchedule OverrideColumnHearder_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule, List<string> fieldNameList, List<string> newFieldNameList)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            IList<ScheduleFieldId> sourceFieldOrder = definition.GetFieldOrder();

            // Refresh data to be sure that schedule is ready for Header Change
            schedule.RefreshData();

            for (int i = 0; i < fieldNameList.Count; i++)
            {
                string fieldName = fieldNameList[i];
                string newFieldName = newFieldNameList[i];
                foreach (ScheduleFieldId fieldId in sourceFieldOrder)
                {
                    Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);


                    //if (foundField.ColumnHeading == fieldName) This works if we had not change the column header
                    if (foundField.GetName() == fieldName)
                    {
                        foundField.ColumnHeading = newFieldName;
                    }
                }

            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        //https://www.revitapidocs.com/2015/3890f745-6f24-f81a-9f8f-d8b47c8e3f94.htm

        /// <summary>
        /// Using the ScheduleField.IsHidden property to hide selected field.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="fieldName">Name of the scheduled field.</param>
        /// <returns name="Schedule">schedule(s) with new header</returns> 
        public static Autodesk.Revit.DB.ViewSchedule HideColumn_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule, string fieldName)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            // clear the sort setting before assign new rules
            //https://www.revitapidocs.com/2019/645645fa-52b8-b747-e3d4-b5428962a4bc.htm
            // If you did not include this line, Revit will add new sort rules for the field and this may cause problem
            // in case users don't want to clear sort filed, we can remove this line or add conditional statement
            definition.ClearSortGroupFields();

            IList<ScheduleFieldId> sourceFieldOrder = definition.GetFieldOrder();

            // Refresh data to be sure that schedule is ready for Header Change
            schedule.RefreshData();
            foreach (ScheduleFieldId fieldId in sourceFieldOrder)
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                //if (foundField.ColumnHeading == fieldName) This works if we had not change the column header
                if (foundField.GetName() == fieldName)
                {
                    foundField.IsHidden = true;
                }
            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;

        }


        /// <summary>
        /// Display all available ScheduleFilterType, you can choose according to Index. 
        /// Possible Error message: The filter value is not valid for the field and filter type.
        /// </summary>
        /// <returns name="ScheduleFilterType">schedule(s) filter type list</returns> 
        public static List<ScheduleFilterType> ScheduleFilterType_ZTN()
        {
            return Enum.GetValues(typeof(ScheduleFilterType)).Cast<ScheduleFilterType>().ToList();
        }


        /// <summary>
        /// Using the ScheduleField.IsHidden property to hide selected field.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="fieldName">Name of the scheduled field.</param>
        /// <returns name="SscheduleFieldParameterID">The ID of the parameter displayed by the field.</returns> 
        /// <returns name="ScheduleFieldID">The ID of the field in the containing ScheduleDefinition</returns>
        /// <returns name="ScheduleFieldIndex">The index of the field in the containing ScheduleDefinition.</returns>
        /// <returns name="ScheduleColumnHeading">The column heading text.</returns>
        /// <returns name="ScheduleFieldType">The type of data displayed by the field.</returns>
        [MultiReturn(new[] { "scheduleFieldParameterID", "scheduleFieldID", "scheduleFieldIndex", "scheduleColumnHeading", "scheduleFieldType" })]
        public static Dictionary<string, object> ScheduleFieldMap_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule, string fieldName)
        {
            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            IList<ScheduleFieldId> sourceFieldOrder = definition.GetFieldOrder();
            Autodesk.Revit.DB.ScheduleField foundField2 = null;

            foreach (ScheduleFieldId fieldId in sourceFieldOrder)
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                //if (foundField.ColumnHeading == fieldName) This works if we had not change the column header
                if (foundField.GetName() == fieldName)
                {
                    foundField2 = foundField;
                }
            }

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            //https://www.revitapidocs.com/2023/a055809c-b743-d17f-aef2-31cfa208ecc1.htm
            Dictionary<string, object> scheduleFieldDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                { "scheduleFieldParameterID", foundField2.ParameterId},
                { "scheduleFieldID", foundField2.FieldId },
                { "scheduleFieldIndex", foundField2.FieldIndex },
                { "scheduleColumnHeading", foundField2.ColumnHeading },
                { "scheduleFieldType", foundField2.FieldType }
            };

            return scheduleFieldDict;
        }

        //https://adndevblog.typepad.com/aec/2015/04/revitapi-scheduledefinitiongetschedulablefields-returns-more-fields-than-ui.html
        /// <summary>
        /// Return all schedulable fields of the selected Schedule. 
        /// Remove  shared or project parameter from field
        /// </summary>
        /// <returns name="SchedulableField">schedulable field of the selected schedule</returns> 
        public static List<Autodesk.Revit.DB.SchedulableField> SchedulableField_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule)
        {
            List<Autodesk.Revit.DB.SchedulableField> scheduleFields = new List<Autodesk.Revit.DB.SchedulableField>();

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            var fields = definition.GetSchedulableFields();
            foreach (var field in fields)
            {
                if (field.ParameterId.IntegerValue < 0)
                {
                    // parameter visible in the UI
                    scheduleFields.Add(field);
                }
                else
                {
                    // shared or project parameter
                }
            }
            return scheduleFields;

        }

        //https://adndevblog.typepad.com/aec/2015/04/revitapi-scheduledefinitiongetschedulablefields-returns-more-fields-than-ui.html
        /// <summary>
        /// Return all schedulable fields of the selected Schedule. 
        /// Remove  shared or project parameter from field
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <returns name="SchedulableFieldName">schedulable field name of the selected schedule</returns> 
        public static List<string> SchedulableFieldName_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            List<string> scheduleFields = new List<string>();

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            var fields = definition.GetSchedulableFields();
            foreach (var field in fields)
            {
                if (field.ParameterId.IntegerValue < 0)
                {
                    // parameter visible in the UI
                    scheduleFields.Add(field.GetName(document));
                }
                else
                {
                    // shared or project parameter
                }
            }
            return scheduleFields;
        }

        /// <summary>
        /// Return all selected schedulable fields of the selected Schedule. 
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <returns name="ScheduledField">scheduled Fields of selected Schedule</returns> 
        public static List<Autodesk.Revit.DB.ScheduleField> ScheduledField_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule)
        {
            List<Autodesk.Revit.DB.ScheduleField> scheduledFields = new List<Autodesk.Revit.DB.ScheduleField>();

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            foreach (ScheduleFieldId fieldId in definition.GetFieldOrder())
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                scheduledFields.Add(foundField);
            }

            return scheduledFields;
        }

        /// <summary>
        /// Return all selected schedulable fields of the selected Schedule. 
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <returns name="ScheduledFieldName">scheduled Field name of selected Schedule</returns> 
        public static List<string> ScheduledFieldName_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule)
        {
            List<string> scheduledFields = new List<string>();

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            foreach (ScheduleFieldId fieldId in definition.GetFieldOrder())
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                scheduledFields.Add(foundField.GetName());
            }

            return scheduledFields;
        }


        /// <summary>
        /// Return all schedulable fields and scheduled fields of the selected Schedule. 
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <returns name="SchedulableFieldName">scheduled Field name of selected Schedule</returns> 
        /// <returns name="ScheduledFieldName">scheduled Field name of selected Schedule</returns> 
        [MultiReturn(new[] { "schedulableFields", "scheduledFields" })]
        public static Dictionary<string, object> ScheduleFields_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            List<string> schedulableFields = new List<string>();
            List<string> scheduledFields = new List<string>();

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            foreach (var field in definition.GetSchedulableFields())
            {
                if (field.ParameterId.IntegerValue < 0)
                {
                    // parameter visible in the UI
                    schedulableFields.Add(field.GetName(document));
                }
                else
                {
                    // shared or project parameter
                }
            }

            foreach (ScheduleFieldId fieldId in definition.GetFieldOrder())
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                scheduledFields.Add(foundField.GetName());
            }

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, object> scheduleFieldDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                { "schedulableFields", schedulableFields },
                { "scheduledFields", scheduledFields }
            };

            return scheduleFieldDict;
        }

        #region "Shared Parameters"
        //https://forums.autodesk.com/t5/revit-api-forum/adding-a-shared-parameter/td-p/6015818
        //https://forums.autodesk.com/t5/revit-api-forum/shared-parameter-as-project-parameter/td-p/6833022
        //https://forums.autodesk.com/t5/revit-api-forum/revit-2022-parametertype-text-to-forgetypeid/m-p/10231439#M55099
        /// <summary>
        /// Add project shared parameters and assign to single category,this methods works for Revit 2021 and later versions.
        /// From SDK worked example DoorSwing: DoorSharedParameter.CS
        /// </summary>
        /// <param name="paraName">New defined shared Parameter Name.</param>
        /// <param name="groupName">The shared parameter group which is defined at Edit Shared Parameters dialog.</param>
        /// <param name="paraType">Type of Parameter choosen from Parameter Properties dialog, for example text or int or YesNo.</param>
        /// <param name="dynamoCategory">Category of the shared parameter applied to, for example Structural Columns, Doors.</param>
        /// <param name="builtInParaGroupName">Information from Group Parameter Under,built-in parameter groups supported by Autodesk Revit, for example PG_TEXT, PG_IDENTITY_DATA.</param>
        /// <param name="instanceOrType">ture means family instance parameter, false means family type parameter.</param>
        public static void AddSharedParameters_1Category_ZTN(string paraName, string groupName, string paraType,
            global::Revit.Elements.Category dynamoCategory, string builtInParaGroupName, bool instanceOrType = true)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
            UIApplication uiapp = RevitServices.Persistence.DocumentManager.Instance.CurrentUIApplication;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            // Create a new Binding object with the categories to which the parameter will be bound.
            CategorySet categories = uiapp.Application.Create.NewCategorySet();

            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories01 = document.Settings.Categories;
            Autodesk.Revit.DB.Category revitCategory = categories01.get_Item(dynamoCategory.Name);
            BuiltInCategory builtInCategory = (BuiltInCategory)revitCategory.Id.IntegerValue;

            // get category and insert into the CategorySet. Apply the shared parameters to multiple categories
            categories.Insert(revitCategory);

            // Open the shared parameters file 
            // via the private method AccessOrCreateSharedParameterFile
            DefinitionFile defFile = AccessOrCreateSharedParameterFile(uiapp.Application);
            if (null == defFile)
            {
                return;
            }

            // Access an existing or create a new group in the shared parameters file
            DefinitionGroups defGroups = defFile.Groups;
            DefinitionGroup defGroup = defGroups.get_Item(groupName);
            if (null == defGroup)
            {
                defGroup = defGroups.Create(groupName);
            }


            // Access an existing or create a new external parameter definition belongs to a specific group.
            //https://www.revitapidocs.com/2023/449e1cdb-ae48-6474-4da5-979c14b408f8.htm
            // for "newSharedPara"
            if (!AlreadyAddedSharedParameter(document, paraName, builtInCategory))
            {
                Definition newSharedPara = defGroup.Definitions.get_Item(paraName);

                if (null == newSharedPara)
                {
                    //https://www.revitapidocs.com/2023/449e1cdb-ae48-6474-4da5-979c14b408f8.htm
                    ForgeTypeId forgeTypeId = ForgeTypeIdByParameterTypeName_ZTN(paraType);
                    ExternalDefinitionCreationOptions ExternalDefinitionCreationOptions = new ExternalDefinitionCreationOptions(paraName, forgeTypeId);
                    //SpecTypeId.String.Text
                    newSharedPara = defGroup.Definitions.Create(ExternalDefinitionCreationOptions);
                }

                // create one instance binding if true; and one type binding if false
                //Use an InstanceBinding or a TypeBinding object to create a new Binding object
                //that includes the categories to which the parameter is bound
                BuiltInParameterGroup builtInParaGroup = BuiltInParameterGroupAccordingToName_ZTN(builtInParaGroupName);
                if (instanceOrType)
                {
                    //Parameter binding connects a parameter definition to elements within one or more categories
                    InstanceBinding paraBinding = uiapp.Application.Create.NewInstanceBinding(categories);
                    //BindingMap bindingMap = uiapp.ActiveUIDocument.Document.ParameterBindings;
                    Autodesk.Revit.DB.BindingMap bindingMap = document.ParameterBindings;
                    // Add the binding and definition to the document.
                    bindingMap.Insert(newSharedPara, paraBinding, builtInParaGroup);
                }
                else
                {
                    TypeBinding paraBinding = uiapp.Application.Create.NewTypeBinding(categories);
                    //BindingMap bindingMap = uiapp.ActiveUIDocument.Document.ParameterBindings;
                    BindingMap bindingMap = document.ParameterBindings;
                    // Add the binding and definition to the document.
                    bindingMap.Insert(newSharedPara, paraBinding, builtInParaGroup);
                }

            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

        }


        /// <summary>
        /// Add shared parameters needed in this sample, ,this methods works for Revit 2021 and later versions.
        /// From SDK worked example DoorSwing: DoorSharedParameter.CS
        /// </summary>
        /// <param name="paraName">New defined shared Parameter Name.</param>
        /// <param name="groupName">The shared parameter group which is defined at Edit Shared Parameters dialog.</param>
        /// <param name="paraType">Type of Parameter choosen from Parameter Properties dialog, for example text or int or YesNo.</param>
        /// <param name="dynamoCategoryList">Category of the shared parameter applied to, for example Structural Columns, Doors.</param>
        /// <param name="builtInParaGroupName">Information from Group Parameter Under,built-in parameter groups supported by Autodesk Revit, for example PG_TEXT, PG_IDENTITY_DATA.</param>
        /// <param name="instanceOrType">ture means family instance parameter, false means family type parameter.</param>
        public static void AddSharedParametersToCategoryList_ZTN(string paraName, string groupName, string paraType,
            List<global::Revit.Elements.Category> dynamoCategoryList, string builtInParaGroupName, bool instanceOrType = true)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
            UIApplication uiapp = RevitServices.Persistence.DocumentManager.Instance.CurrentUIApplication;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            // Open the shared parameters file 
            // via the private method AccessOrCreateSharedParameterFile
            DefinitionFile defFile = AccessOrCreateSharedParameterFile(uiapp.Application);
            if (null == defFile)
            {
                return;
            }

            // Access an existing or create a new group in the shared parameters file
            DefinitionGroups defGroups = defFile.Groups;
            DefinitionGroup defGroup = defGroups.get_Item(groupName);
            if (null == defGroup)
            {
                defGroup = defGroups.Create(groupName);
            }

            // Create a new Binding object with the categories to which the parameter will be bound.
            CategorySet categories = uiapp.Application.Create.NewCategorySet();
            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories01 = document.Settings.Categories;

            foreach (global::Revit.Elements.Category dynamoCategory in dynamoCategoryList)
            {
                Autodesk.Revit.DB.Category revitCategory = categories01.get_Item(dynamoCategory.Name);
                // get category and insert into the CategorySet. Apply the shared parameters to multiple categories
                categories.Insert(revitCategory);
            }

            foreach (global::Revit.Elements.Category dynamoCategory in dynamoCategoryList)
            {
                Autodesk.Revit.DB.Category revitCategory = categories01.get_Item(dynamoCategory.Name);
                BuiltInCategory builtInCategory = (BuiltInCategory)revitCategory.Id.IntegerValue;

                //如果Paraname已存在，但是不存在于builtInCategory里，仅勾选category
                //如果Paraname不存在，创建参数并勾选category
                // create one instance binding if true; and one type binding if false
                //Use an InstanceBinding or a TypeBinding object to create a new Binding object
                //that includes the categories to which the parameter is bound                
                //Parameter binding connects a parameter definition to elements within one or more categories
                InstanceBinding instanceBinding = uiapp.Application.Create.NewInstanceBinding(categories);
                TypeBinding typeBinding = uiapp.Application.Create.NewTypeBinding(categories);

                //BindingMap bindingMap = uiapp.ActiveUIDocument.Document.ParameterBindings;
                Autodesk.Revit.DB.BindingMap bindingMap = document.ParameterBindings;

                // Access an existing or create a new external parameter definition belongs to a specific group.
                //https://www.revitapidocs.com/2023/449e1cdb-ae48-6474-4da5-979c14b408f8.htm
                // for "newSharedPara"      问题估计出在这里
                //如果Paraname已存在，builtInCategory已勾选，不做任何操作
                if (!AlreadyAddedSharedParameter(document, paraName, builtInCategory))
                {
                    Definition newSharedPara = defGroup.Definitions.get_Item(paraName);

                    //Create new shared parameter if not exist in the .txt file
                    if (null == newSharedPara)
                    {
                        //https://www.revitapidocs.com/2023/449e1cdb-ae48-6474-4da5-979c14b408f8.htm
                        ForgeTypeId forgeTypeId = ForgeTypeIdByParameterTypeName_ZTN(paraType);
                        ExternalDefinitionCreationOptions ExternalDefinitionCreationOptions = new ExternalDefinitionCreationOptions(paraName, forgeTypeId);
                        //create new shared parameter
                        newSharedPara = defGroup.Definitions.Create(ExternalDefinitionCreationOptions);
                    }

                    BuiltInParameterGroup builtInParaGroup = BuiltInParameterGroupAccordingToName_ZTN(builtInParaGroupName);
                    //https://www.revitapidocs.com/2022/c3bed87a-956f-47c3-060c-0294c7ef43e7.htm
                    if (instanceOrType)
                    {
                        // Add the binding and definition to the document.
                        bindingMap.Insert(newSharedPara, instanceBinding, builtInParaGroup);
                    }
                    else
                    {
                        // Add the binding and definition to the document.
                        bindingMap.Insert(newSharedPara, typeBinding, builtInParaGroup);
                    }
                }

            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();
        }


        /*
        //https://forums.autodesk.com/t5/revit-api-forum/adding-a-shared-parameter/td-p/6015818
        //https://forums.autodesk.com/t5/revit-api-forum/shared-parameter-as-project-parameter/td-p/6833022
        //https://forums.autodesk.com/t5/revit-api-forum/revit-2022-parametertype-text-to-forgetypeid/m-p/10231439#M55099           
        /// <summary>
        /// Add shared parameters needed in this sample, this methods works for Revit 2022 and former versions.
        /// </summary>
        /// <param name="paraName">New defined shared Parameter Name.</param>
        /// <param name="groupName">The shared parameter group which is defined at Edit Shared Parameters dialog.</param>
        /// <param name="paraType">Type of Parameter choosen from Parameter Properties dialog.</param>
        /// <param name="dynamoCategory">Category of the shared parameter applied to.</param>
        /// <param name="builtInParaGroup">Information from Group Parameter Under.</param>
        /// <param name="instanceOrType">ture means family instance parameter, false means family type parameter.</param>
        public static void AddSharedParameters22_ZTN(string paraName, string groupName, ParameterType paraType,
            Revit.Elements.Category dynamoCategory, BuiltInParameterGroup builtInParaGroup, bool instanceOrType)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
            UIApplication uiapp = RevitServices.Persistence.DocumentManager.Instance.CurrentUIApplication;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            // Create a new Binding object with the categories to which the parameter will be bound.
            CategorySet categories = uiapp.Application.Create.NewCategorySet();

            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories01 = document.Settings.Categories;
            Autodesk.Revit.DB.Category revitCategory = categories01.get_Item(dynamoCategory.Name);
            BuiltInCategory builtInCategory = (BuiltInCategory)revitCategory.Id.IntegerValue;

            // get category and insert into the CategorySet.
            categories.Insert(revitCategory);

            // Open the shared parameters file 
            // via the private method AccessOrCreateSharedParameterFile
            DefinitionFile defFile = AccessOrCreateSharedParameterFile(uiapp.Application);
            if (null == defFile)
            {
                return;
            }

            // Access an existing or create a new group in the shared parameters file
            DefinitionGroups defGroups = defFile.Groups;
            DefinitionGroup defGroup = defGroups.get_Item(groupName);

            if (null == defGroup)
            {
                defGroup = defGroups.Create(groupName);
            }


            // Access an existing or create a new external parameter definition belongs to a specific group.
            //https://www.revitapidocs.com/2022/e31e22aa-179f-322b-7ae5-f3f840cf4158.htm

            // for "newSharedPara"
            if (!AlreadyAddedSharedParameter(document, paraName, builtInCategory))
            {
                Definition newSharedPara = defGroup.Definitions.get_Item(paraName);

                if (null == newSharedPara)
                {
                    //https://www.revitapidocs.com/2022/e31e22aa-179f-322b-7ae5-f3f840cf4158.htm
                    ExternalDefinitionCreationOptions ExternalDefinitionCreationOptions = new ExternalDefinitionCreationOptions(paraName, paraType);
                    //SpecTypeId.String.Text
                    newSharedPara = defGroup.Definitions.Create(ExternalDefinitionCreationOptions);
                }

                // create one instance binding if true;
                // and one type binding if false
                if (instanceOrType)
                {
                    InstanceBinding paraBinding = uiapp.Application.Create.NewInstanceBinding(categories);
                    //BindingMap bindingMap = uiapp.ActiveUIDocument.Document.ParameterBindings;
                    Autodesk.Revit.DB.BindingMap bindingMap = document.ParameterBindings;
                    // Add the binding and definition to the document.
                    bindingMap.Insert(newSharedPara, paraBinding, builtInParaGroup);
                }
                else
                {
                    TypeBinding paraBinding = uiapp.Application.Create.NewTypeBinding(categories);
                    //BindingMap bindingMap = uiapp.ActiveUIDocument.Document.ParameterBindings;
                    BindingMap bindingMap = document.ParameterBindings;
                    // Add the binding and definition to the document.
                    bindingMap.Insert(newSharedPara, paraBinding, builtInParaGroup);
                }

            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

        }
        */

        /// <summary>
        /// Access an existing or create a new shared parameters file.
        /// </summary>
        /// <param name="app">Revit Application.</param>
        /// <returns>the shared parameters file.</returns>
        private static DefinitionFile AccessOrCreateSharedParameterFile(Autodesk.Revit.ApplicationServices.Application app)
        {
            // The location of this command assembly
            string currentCommandAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // The path of ourselves shared parameters file
            string sharedParameterFilePath = Path.GetDirectoryName(currentCommandAssemblyPath);
            sharedParameterFilePath += "\\Shared Parameter.txt";

            // Check if the file exits
            System.IO.FileInfo documentMessage = new FileInfo(sharedParameterFilePath);
            bool fileExist = documentMessage.Exists;

            // Create file for external shared parameter since it does not exist
            if (!fileExist)
            {
                FileStream fileFlow = File.Create(sharedParameterFilePath);
                fileFlow.Close();
            }

            // Set ourselves file to the externalSharedParameterFile 
            app.SharedParametersFilename = sharedParameterFilePath;
            //Method's return
            DefinitionFile sharedParameterFile = app.OpenSharedParameterFile();

            return sharedParameterFile;
        }

        /// <summary>
        /// Has the specific document shared parameter already been added ago?
        /// </summary>
        /// <param name="doc">Revit project in which the shared parameter will be added.</param>
        /// <param name="paraName">the name of the shared parameter.</param>
        /// <param name="boundCategory">Which category the parameter will bind to</param>
        /// <returns>Returns true if already added ago else returns false.</returns>
        private static bool AlreadyAddedSharedParameter(Document doc, string paraName, BuiltInCategory boundCategory)
        {
            try
            {
                BindingMap bindingMap = doc.ParameterBindings;
                DefinitionBindingMapIterator bindingMapIter = bindingMap.ForwardIterator();

                while (bindingMapIter.MoveNext())
                {
                    //find the parameter according to name
                    if (bindingMapIter.Key.Name.Equals(paraName))
                    {
                        //find all checked category related to this shared parameter
                        ElementBinding binding = bindingMapIter.Current as ElementBinding;
                        CategorySet categories = binding.Categories;

                        foreach (Autodesk.Revit.DB.Category category in categories)
                        {
                            //这里一种特殊情况：ParaName已经存在，但是boundCategory没勾选，就会返回False
                            //这种情况会导致不创建Parameter，但是补充勾选Category
                            if (category.Id.IntegerValue.Equals((int)boundCategory))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }


        //https://www.revitapidocs.com/2023/9942b791-2892-0658-303e-abf99675c5a6.htm
        /// <summary>
        /// An enumerated type listing all of the built-in parameter groups supported by Autodesk Revit. 
        /// such as ["PG_GRAPHICS", "PG_IDENTITY_DATA", "PG_CONSTRAINTS"] 
        /// </summary>
        /// <returns name="BuiltInParameterGroup">BuiltInParameterGroup list</returns> 
        public static List<BuiltInParameterGroup> BuiltInParameterGroup_ZTN()
        {
            return Enum.GetValues(typeof(BuiltInParameterGroup)).Cast<BuiltInParameterGroup>().ToList();
        }


        //https://www.revitapidocs.com/2023/9942b791-2892-0658-303e-abf99675c5a6.htm
        /// <summary>
        /// An enumerated type listing all of the built-in parameter groups supported by Autodesk Revit. 
        /// such as ["PG_GRAPHICS", "PG_IDENTITY_DATA", "PG_CONSTRAINTS"] 
        /// </summary>
        /// <returns name="BuiltInParameterGroup">BuiltInParameterGroup list</returns> 
        private static BuiltInParameterGroup BuiltInParameterGroupAccordingToName_ZTN(string builtInParameterGroupName = "PG_IDENTITY_DATA")
        {
            List<BuiltInParameterGroup> group = Enum.GetValues(typeof(BuiltInParameterGroup)).Cast<BuiltInParameterGroup>().ToList();

            BuiltInParameterGroup builtInParameterGroup = new BuiltInParameterGroup();
            foreach (BuiltInParameterGroup member in group)
            {
                if (member.ToString() == builtInParameterGroupName)
                {
                    builtInParameterGroup = member;
                }

            }

            return builtInParameterGroup;

        }

        //https://www.revitapidocs.com/2023/87de2c69-a5e8-40e3-3d7a-9b18f1fda03a.htm
        /// <summary>
        /// Revit 2021 deprecated the UnitType property and replaced it with the GetSpecTypeId method.
        /// Revit 2022 deprecated the ParameterType property and the GetSpecTypeId method, replacing them both with the GetDataType method.
        /// </summary>
        /// <returns name="SpecTypeIdProperty Name">SpecTypeId Properties Name</returns> 
        /// <returns name="forgeTypeId">Special SpecTypeId Properties, like SpecTypeId.String.Text, or SpecTypeId.Boolean.YesNo</returns> 
        private static Dictionary<string, ForgeTypeId> ForgeTypeIdDict_ZTN()
        {

            List<string> specTypeIdPropertyNameList = new List<string>();
            List<ForgeTypeId> forgeTypeIdList = new List<ForgeTypeId>();

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, ForgeTypeId> forgeTypeIdDict = new Dictionary<string, ForgeTypeId>();

            //https://forums.autodesk.com/t5/revit-api-forum/convert-parametertype-fixtureunit-to-forgetypeid/td-p/10268488
            PropertyInfo[] props = typeof(SpecTypeId).GetProperties();
            foreach (PropertyInfo pi in props)
            {
                specTypeIdPropertyNameList.Add(pi.Name);

                var forgeTypeId = pi.GetValue(null) as ForgeTypeId;
                forgeTypeIdList.Add(forgeTypeId);

                forgeTypeIdDict.Add(pi.Name, forgeTypeId);
            }

            //https://www.revitapidocs.com/2023/cd7d3c3d-b476-9579-1a30-b6b82f1a66d7.htm
            specTypeIdPropertyNameList.Add("Text");
            forgeTypeIdList.Add(SpecTypeId.String.Text);
            specTypeIdPropertyNameList.Add("Material");
            forgeTypeIdList.Add(SpecTypeId.Reference.Material);
            //https://www.revitapidocs.com/2023/3f507360-05c2-b25f-df4f-06f104fb0a6b.htm
            specTypeIdPropertyNameList.Add("Integer");
            forgeTypeIdList.Add(SpecTypeId.Int.Integer);
            specTypeIdPropertyNameList.Add("YesNo");
            forgeTypeIdList.Add(SpecTypeId.Boolean.YesNo);

            forgeTypeIdDict.Add("Text", SpecTypeId.String.Text);
            forgeTypeIdDict.Add("Material", SpecTypeId.Reference.Material);
            forgeTypeIdDict.Add("Integer", SpecTypeId.Int.Integer);
            forgeTypeIdDict.Add("YesNo", SpecTypeId.Boolean.YesNo);

            return forgeTypeIdDict;
        }

        private static ForgeTypeId ForgeTypeIdByParameterTypeName_ZTN(string forgeTypeIdName)
        {
            //var forgeTypeId = new ForgeTypeIdDict_ZTN();
            return ForgeTypeIdDict_ZTN()[forgeTypeIdName];
        }



        /*
        /// <summary>
        /// Revit 2021 deprecated the UnitType property and replaced it with the GetSpecTypeId method.
        /// Revit 2022 deprecated the ParameterType property and the GetSpecTypeId method, replacing them both with the GetDataType method.
        /// </summary>
        /// <returns name="SpecTypeIdProperty Name">SpecTypeId Properties Name</returns> 
        /// <returns name="forgeTypeId">Special SpecTypeId Properties, like SpecTypeId.String.Text, or SpecTypeId.Boolean.YesNo</returns> 
        private static ForgeTypeId ForgeTypeIdByParameterTypeName_ZTN(string forgeTypeIdName)
        {

            List<string> specTypeIdPropertyNameList = new List<string>();
            List<ForgeTypeId> forgeTypeIdList = new List<ForgeTypeId>();

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, ForgeTypeId> forgeTypeIdDict = new Dictionary<string, ForgeTypeId>();

            //https://forums.autodesk.com/t5/revit-api-forum/convert-parametertype-fixtureunit-to-forgetypeid/td-p/10268488
            PropertyInfo[] props = typeof(SpecTypeId).GetProperties();
            foreach (PropertyInfo pi in props)
            {
                specTypeIdPropertyNameList.Add(pi.Name);

                var forgeTypeId = pi.GetValue(null) as ForgeTypeId;
                forgeTypeIdList.Add(forgeTypeId);

                forgeTypeIdDict.Add(forgeTypeIdName, forgeTypeId);
            }

            //https://www.revitapidocs.com/2023/cd7d3c3d-b476-9579-1a30-b6b82f1a66d7.htm
            specTypeIdPropertyNameList.Add("Text");
            forgeTypeIdList.Add(SpecTypeId.String.Text);
            specTypeIdPropertyNameList.Add("Material");
            forgeTypeIdList.Add(SpecTypeId.Reference.Material);
            //https://www.revitapidocs.com/2023/3f507360-05c2-b25f-df4f-06f104fb0a6b.htm
            specTypeIdPropertyNameList.Add("Integer");
            forgeTypeIdList.Add(SpecTypeId.Int.Integer);
            specTypeIdPropertyNameList.Add("YesNo");
            forgeTypeIdList.Add(SpecTypeId.Boolean.YesNo);

            forgeTypeIdDict.Add("Text", SpecTypeId.String.Text);
            forgeTypeIdDict.Add("Material", SpecTypeId.Reference.Material);
            forgeTypeIdDict.Add("Integer", SpecTypeId.Int.Integer);
            forgeTypeIdDict.Add("YesNo", SpecTypeId.Boolean.YesNo);

            return forgeTypeIdDict[forgeTypeIdName];
        }



        //https://jeremytammik.github.io/tbc/a/1908_forgetypeid.html
        //https://www.revitapidocs.com/2022/f38d847e-207f-b59a-3bd6-ebea80d5be63.htm
        //https://forums.autodesk.com/t5/revit-api-forum/internal-definition-parameter-type-missing-in-revit-2023/td-p/11092148
        //https://forums.autodesk.com/t5/revit-api-forum/revit-2022-parametertype-text-to-forgetypeid/m-p/10225741
        //https://forums.autodesk.com/t5/revit-api-forum/forgetypeid-how-to-use/td-p/9439210
        //https://jeremytammik.github.io/tbc/a/1902_2022_sdk_tbc.html
        /// <summary>
        /// An enumerated type listing all of the data type interpretation that Autodesk Revit supports. 
        /// such as ["Text", "Length", "Volume"], works for Revit 2022 and former versions. 
        /// </summary>
        /// <returns name="ParameterType">BuiltInParameterGroup list</returns> 
        public static List<ParameterType> ParameterType_ZTN()
        {
            return Enum.GetValues(typeof(ParameterType)).Cast<ParameterType>().ToList();
        } 
        */

        //https://www.revitapidocs.com/2022/a93168f7-b52d-e97a-7935-50ddcec7fb54.htm
        //https://github.com/DynamoDS/DynamoRevit/blob/217f4a6ed7419920f03e3196d5f3206fb9ccdefb/src/Libraries/RevitNodesUI/RevitTypes.cs#L124
        //https://www.revitapidocs.com/2023/87de2c69-a5e8-40e3-3d7a-9b18f1fda03a.htm
        //https://forums.autodesk.com/t5/revit-api-forum/convert-parametertype-fixtureunit-to-forgetypeid/td-p/10268488
        //https://www.revitapidocs.com/2022/a4ee065a-21d5-3948-7cd8-1599c4d0dc40.htm
        //https://www.revitapidocs.com/2022/5f0e82b9-cf62-062d-5136-3c4032cca766.htm
        // https://apidocs.co/apps/revit/2022/a2817756-7d35-f9b9-0daf-172010b66ed0.htm
        //https://forums.autodesk.com/t5/revit-api-forum/globalparameter-create-error-spectypeid-revit-22-0-2-392/td-p/10998925
        //https://jeremytammik.github.io/tbc/a/1902_2022_sdk_tbc.html
        /// <summary>
        /// Revit 2021 deprecated the UnitType property and replaced it with the GetSpecTypeId method.
        /// Revit 2022 deprecated the ParameterType property and the GetSpecTypeId method, replacing them both with the GetDataType method.
        /// </summary>
        /// <returns name="SpecTypeIdProperty Name">SpecTypeId Properties Name</returns> 
        /// <returns name="forgeTypeId">Special SpecTypeId Properties, like SpecTypeId.String.Text, or SpecTypeId.Boolean.YesNo</returns> 
        [MultiReturn(new[] { "specTypeIdPropertyName", "forgeTypeId" })]
        //"specTypeIdName", "specTypeId", 
        public static object SpecTypeId_ZTN()
        {
            //List<ForgeTypeId> forgeTypeIdList = new List<ForgeTypeId>();
            //List<string> forgeTypeIdNameList = new List<string>();
            //List<PropertyInfo> specTypeIdPropertyList = new List<PropertyInfo>();
            //List<PropertyInfo> specTypeIdPropertyList = new List<PropertyInfo>();
            List<string> specTypeIdPropertyNameList = new List<string>();
            //List<ForgeTypeId> specialSpecTypeIdList = new List<ForgeTypeId>();
            List<ForgeTypeId> forgeTypeIdList = new List<ForgeTypeId>();

            //foreach (ForgeTypeId forgeTypeId in UnitUtils.GetAllMeasurableSpecs())
            //{
            //    forgeTypeIdList.Add(forgeTypeId);
            //    forgeTypeIdNameList.Add(forgeTypeId.TypeId);

            //    //specTypeId.TypeId.ToString();
            //}


            //https://forums.autodesk.com/t5/revit-api-forum/convert-parametertype-fixtureunit-to-forgetypeid/td-p/10268488
            PropertyInfo[] props = typeof(SpecTypeId).GetProperties();
            foreach (PropertyInfo pi in props)
            {
                //specTypeIdPropertyList.Add(pi);
                specTypeIdPropertyNameList.Add(pi.Name);

                var forgeTypeId = pi.GetValue(null) as ForgeTypeId;
                forgeTypeIdList.Add(forgeTypeId);
            }

            //https://www.revitapidocs.com/2023/cd7d3c3d-b476-9579-1a30-b6b82f1a66d7.htm
            specTypeIdPropertyNameList.Add("Text");
            forgeTypeIdList.Add(SpecTypeId.String.Text);
            specTypeIdPropertyNameList.Add("Material");
            forgeTypeIdList.Add(SpecTypeId.Reference.Material);
            //https://www.revitapidocs.com/2023/3f507360-05c2-b25f-df4f-06f104fb0a6b.htm
            specTypeIdPropertyNameList.Add("Integer");
            forgeTypeIdList.Add(SpecTypeId.Int.Integer);
            specTypeIdPropertyNameList.Add("YesNo");
            forgeTypeIdList.Add(SpecTypeId.Boolean.YesNo);

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, object> specTypeIdDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                //{ "specTypeIdProperty",specTypeIdPropertyList},
                { "specTypeIdPropertyName",specTypeIdPropertyNameList},
                //{ "specialSpecTypeId",specialSpecTypeIdList},
                { "forgeTypeId", forgeTypeIdList }
            };

            return specTypeIdDict;
        }

        #endregion
    }

}

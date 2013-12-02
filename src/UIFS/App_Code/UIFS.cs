using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Principal;

namespace UIFS
{
    /*** UIFS - User Interface Form System
     * 
     * 
     * 
     */

    public class FormLIST
    {
        public int id;
        public Int16 currentversion;
        public string name;
        public string description;
        public bool active;
        public DateTime created;
        public string createdby;
        public DateTime lastmodified;
        public string lastmodifiedby;
    }


    // This is our "FORM" class
    public class FormDataStruct
    {
        // Main form detail
        public int id = 0; // id (default to zero which represents a new form, loaded forms will have the actual identifier)
        public string name; // name of form (varchar_255)
        public string description; // desc of form
        public int version; // The current version of the form
        public DateTime created; // Creation Date
        public bool newform = true; // If a form is loaded this is set to false
        public Layout Layout = new Layout(); // The form's layout settings
        // Controls
        public int controls = 0;
        public int nextcontrolid = 0; // will hold the next available identifier to use for a form control (pops on form load)
        public ControlListDetail[] ControlList;
        public jQuery jQuery = new jQuery(); // to build jquery for ajax calls
        // Control type arrays
        public FormControl.Textbox[] Textbox;
        public FormControl.List[] List;
        public FormControl.Checkbox[] Checkbox;
        public FormControl.DateTime[] DateTime;
        public FormControl.Number[] Number;
        public FormControl.Percentage[] Percentage;
        public FormControl.Range[] Range;


        public class ControlListDetail
        {
            public int id; // the unique db identifier for this control (populated when read from db and used to facillitate loading of form controls)
            public ControlType type; // The type of control
            public int ordernum; // The order # for how it shows on the form
            public int version; // control's version we are working with
            public int index; // the index of the control in its control type array
            public bool controlchanged = false; // If the user changes a control, this flag is set to have the changes saved to the DB when the time comes
            public bool newversionneeded = false; // If the control needs a new version
            public bool added = false; // If the control has been added during this session (not saved)
            public bool removed = false; // If the control has been removed during this session (not saved)
            // Layout properties
            public Layout.ControlProperties Layout = new Layout.ControlProperties();

        }

        // ------------------------------------------------------------
        #region CONTROL ARRAY FUNCTIONS
        public int Find_ControlListEntry_byOrdernum(int ordernum)
        {
            for (int t = 0; t < ControlList.Length; t++)
            {
                if (ControlList[t].ordernum == ordernum)
                {
                    return t;
                }
            }
            return -1;
        }
        public int Find_ControlListEntry_byControlID(int id)
        {
            for (int t = 0; t < ControlList.Length; t++)
            {
                if (ControlList[t].id == id)
                {
                    return t;
                }
            }
            return -1;
        }
        public int Find_Controlindex_byID(int id)
        {
            for (int t = 0; t < ControlList.Length; t++)
            {
                if (ControlList[t].id == id)
                {
                    return ControlList[t].index;
                }
            }
            return -1;
        }
        public int Get_NextOrderNum()
        { // Returns the next highest possible order number for a control (for adding controls to a form)
            int ordernum = 0, highestnum = 1;
            for (int i = 0; i < ControlList.Length; i++)
            {
                ordernum = ControlList[i].ordernum;
                if (ordernum >= highestnum) { highestnum = ordernum + 1; }
            }
            return highestnum;
        }
        public FormControl Get_Control(int Controlid)
        {
            int iControlList = Find_ControlListEntry_byControlID(Controlid);
            int iControl = ControlList[iControlList].index;
            
            switch (ControlList[iControlList].type)
            {
                case ControlType.Textbox:
                    return Textbox[iControl];
                case ControlType.List:
                    return List[iControl];
                case ControlType.Checkbox:
                    return Checkbox[iControl];
                case ControlType.DateTime:
                    return DateTime[iControl];
                case ControlType.Number:
                    return Number[iControl];
                case ControlType.Percentage:
                    return Percentage[iControl];
                case ControlType.Range:
                    return Range[iControl];
            }
            return new FormControl();
        }

        
        #endregion CONTROL ARRAY FUNCTIONS
        // ------------------------------------------------------------


        // Adds a control, and returns the index if not a new control
        public int AddControl(ControlType type, FormControl Control, bool newControl)
        {
            int iControl = 0, newControlListentry=this.controls;
            if (newControl) { 
                // We need to create a ControlList entry
                this.controls += 1; // Update control count
                Array.Resize(ref ControlList, controls); // resize control list
                ControlList[newControlListentry] = new FormDataStruct.ControlListDetail();
                ControlList[newControlListentry].type = type;
                // get and assign needed vars
                Control.id = this.nextcontrolid; nextcontrolid += 1; // get an official control id and increase the count
                Control.ordernum = Get_NextOrderNum();
                ControlList[newControlListentry].ordernum = Control.ordernum; // New controls need an order num (will get sorted later if user dropped in a different order)                
                ControlList[newControlListentry].id = Control.id; // this id assignment is temporary for new (unsaved) controls (when saved to db the id will get assigned)
                Control.version = 1; // new controls start with version 1
            }
            switch (type)
            {
                case ControlType.Textbox:
                    if (Textbox == null)
                    {
                        Array.Resize(ref Textbox, 1); // start new
                    }
                    else
                    {
                        Array.Resize(ref Textbox, Textbox.Length + 1);
                    }
                    iControl = Textbox.Length - 1;
                    Textbox[iControl] = (FormControl.Textbox)Control;
                    break;
                case ControlType.List:
                    if (List == null)
                    {
                        Array.Resize(ref List, 1);
                    }
                    else
                    {
                        Array.Resize(ref List, List.Length + 1);
                    }
                    iControl = List.Length - 1;
                    List[iControl] = (FormControl.List)Control;
                    break;
                case ControlType.Checkbox:
                    if (Checkbox == null)
                    {
                        Array.Resize(ref Checkbox, 1);
                    }
                    else
                    {
                        Array.Resize(ref Checkbox, Checkbox.Length + 1);
                    }
                    iControl = Checkbox.Length - 1;
                    Checkbox[iControl] = (FormControl.Checkbox)Control;
                    break;
                case ControlType.DateTime:
                    if (DateTime == null)
                    {
                        Array.Resize(ref DateTime, 1);
                    }
                    else
                    {
                        Array.Resize(ref DateTime, DateTime.Length + 1);
                    }
                    iControl = DateTime.Length - 1;
                    DateTime[iControl] = (FormControl.DateTime)Control;
                    break;
                case ControlType.Number:
                    if (Number == null)
                    { Array.Resize(ref Number, 1); }
                    else { Array.Resize(ref Number, Number.Length + 1); }
                    iControl = Number.Length - 1;
                    Number[iControl] = (FormControl.Number)Control;
                    break;
                case ControlType.Percentage:
                    if (Percentage == null)
                    { Array.Resize(ref Percentage, 1); }
                    else { Array.Resize(ref Percentage, Percentage.Length + 1); }
                    iControl = Percentage.Length - 1;
                    Percentage[iControl] = (FormControl.Percentage)Control;
                    break;
                case ControlType.Range:
                    if (Range == null)
                    { Array.Resize(ref Range, 1); }
                    else { Array.Resize(ref Range, Range.Length + 1); }
                    iControl = Range.Length - 1;
                    Range[iControl] = (FormControl.Range)Control;
                    break;
            }

            if (newControl)
            { // update index
                ControlList[newControlListentry].index = iControl; // index of new control                
            }

            return iControl; // return the index of the added control
        }

        /* -- RemoveControl: completely destroys a control from existence!
         * 
         */
        public void RemoveControl(int Controlid)
        {
            // Remove control from specific control array list and from ControlList

            // just blank out control details - since it will be removed from the list (saves processing)
            switch (this.ControlList[this.Find_ControlListEntry_byControlID(Controlid)].type)
            {
                case ControlType.Checkbox:
                    this.Checkbox[Find_Controlindex_byID(Controlid)] = null;
                    break;
                case ControlType.DateTime:
                    this.DateTime[Find_Controlindex_byID(Controlid)] = null;
                    break;
                case ControlType.Number:
                    this.Number[Find_Controlindex_byID(Controlid)] = null;
                    break;
                case ControlType.Percentage:
                    this.Percentage[Find_Controlindex_byID(Controlid)] = null;
                    break;
                case ControlType.List:
                    this.List[Find_Controlindex_byID(Controlid)] = null;
                    break;
                case ControlType.Range:
                    this.Range[Find_Controlindex_byID(Controlid)] = null;
                    break;
                case ControlType.Textbox:
                    this.Textbox[Find_Controlindex_byID(Controlid)] = null;
                    break;
            }
            //FormDataStruct.ControlListDetail[] temp_ControlList;
            
            // if it is the ONLY control on the form
            if (ControlList.Length == 1)
            {
                ControlList = null; controls = 0;
            } // otherwise, we walk through array
            else
            {
                // we can leave the array the same until we reach the control to be removed
                int t, a;
                for (t = 0; t < ControlList.Length; t++)
                {
                    if (ControlList[t].id == Controlid)
                    {
                        break;
                    }
                }
                // check if the removed control is at the end of array..otherwise, shift remaining array
                if (t == ControlList.Length - 1)
                {
                    Array.Resize(ref ControlList, t); // remove by resizing without last element
                }
                else
                {
                    // for the remainder, just copy the next over the previous
                    for (a = t; a < ControlList.Length - 1; a++)
                    {
                        ControlList[a] = ControlList[a + 1];
                    }
                    // now resize array 1 size smaller
                    Array.Resize(ref ControlList, a);
                }
                // decrease our controls count
                controls -= 1;
            }
           
        }

        /* Pass to this routine the controlid you want reordered and the new order number
         */
        public void Sort_ControlList(int Controlid, int newOrderNum)
        {
            int oldOrderNum = 0, lowOrderNum, highOrderNum, difference;
            int iControl = 0;
            // First, find original sort number and change to newordernum
            for (int t = 0; t < ControlList.Length; t++)
            {
                if (ControlList[t].id == Controlid)
                {
                    iControl = t;
                    oldOrderNum = ControlList[t].ordernum;  // capture old
                    ControlList[t].ordernum = newOrderNum;  // assign new
                    FormControl ControlChanges = new FormControl();
                    ControlChanges.ordernum = ControlList[t].ordernum;
                    Update_ControlCommonProperties(ControlList[t].id, ControlChanges);
                }
            }
            // Now find the lower/higher between the two values to re-order the controls in-between
            if (newOrderNum > oldOrderNum)
            {
                lowOrderNum = oldOrderNum; highOrderNum = newOrderNum; difference = -1;
            }
            else
            {
                highOrderNum = oldOrderNum; lowOrderNum = newOrderNum; difference = 1;
            }
            // walk through list and reorder the needed controls
            for (int t = 0; t < ControlList.Length; t++)
            { // only reorder if it falls in the correct range and IS NOT the control that is causing the reordereding (already changed)
                if (t != iControl && ControlList[t].ordernum >= lowOrderNum && ControlList[t].ordernum <= highOrderNum)
                { // if in our range, set new ordernum
                    ControlList[t].ordernum = ControlList[t].ordernum + difference;
                    FormControl ControlChanges = new FormControl();
                    ControlChanges.ordernum = ControlList[t].ordernum;
                    Update_ControlCommonProperties(ControlList[t].id, ControlChanges);
                }
            }               

        }
        // ReOrder_ControlList :: just does a complete reorder mapped to control index
        public void ReOrder_ControlList()
        {
            for (int t = 0; t < ControlList.Length; t++)
            {
                ControlList[t].ordernum = t+1;
                FormControl ControlChanges = new FormControl();
                ControlChanges.ordernum = ControlList[t].ordernum;
                Update_ControlCommonProperties(ControlList[t].id, ControlChanges);
            }
        }

        public void Update_ControlCommonProperties(int ControlID, FormControl ControlChanges, bool Controlrequiredchanged = false)
        {
            int iControlList = Find_ControlListEntry_byControlID(ControlID);
            int iControl = ControlList[iControlList].index;

            // Here is where we can check/filter the entered values: name, prompt, tip
            
            // Mark control as changed (if not just addded)
            if (!ControlList[iControlList].added) { ControlList[iControlList].controlchanged = true; }
            switch (ControlList[iControlList].type)
            {
                case ControlType.Textbox:
                    Textbox[iControl].UpdateCommonProperties(ControlChanges);
                    if (Controlrequiredchanged) { Textbox[iControl].required = ControlChanges.required; }
                    break;
                case ControlType.List:
                    List[iControl].UpdateCommonProperties(ControlChanges);
                    if (Controlrequiredchanged) { List[iControl].required = ControlChanges.required; }
                    break;
                case ControlType.Checkbox:
                    Checkbox[iControl].UpdateCommonProperties(ControlChanges);
                    if (Controlrequiredchanged) { Checkbox[iControl].required = ControlChanges.required; }
                    break;
                case ControlType.DateTime:
                    DateTime[iControl].UpdateCommonProperties(ControlChanges);
                    if (Controlrequiredchanged) { DateTime[iControl].required = ControlChanges.required; }
                    break;
                case ControlType.Number:
                    Number[iControl].UpdateCommonProperties(ControlChanges);
                    if (Controlrequiredchanged) { Number[iControl].required = ControlChanges.required; }
                    break;
                case ControlType.Percentage:
                    Percentage[iControl].UpdateCommonProperties(ControlChanges);
                    if (Controlrequiredchanged) { Percentage[iControl].required = ControlChanges.required; }
                    break;
                case ControlType.Range:
                    Range[iControl].UpdateCommonProperties(ControlChanges);
                    if (Controlrequiredchanged) { Range[iControl].required = ControlChanges.required; }
                    break;
                default:
                    // ERROR essentially - log it
                    break;
            }


        }
    }


    /*** --| LAYOUT
     * -------------
     * = This class is for setting the layout options of the form
     * 
     */
    public class Layout
    {
        public Style OutputFormat;

        public enum Style : int
        {
            DIVs = 0, // every control will have its own div and no other formatting
            SingleColumn = 1,
            DoubleColumn = 2
        }

        // This class is assigned to each control
        public class ControlProperties
        {
            public int column = 0; // which column does this control reside in
            public int width; // applies to input controls 'text' width
            public int height;

        }
    }




}

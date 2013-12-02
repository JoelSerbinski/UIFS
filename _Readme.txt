
		--/ UIFS - Universal Interface Form System \--

Licensing: see _License.txt and "license" folder

-- DESC ---------------------------------------------------------------------------------------

  A framework designed for building web applications based "Forms" for input.  The goal is automating as much of the stack as possible.  This includes the jQuery-based front-end, form validation, and back-end database and reporting work.  Dynamic HTML and Javascript are heavily computed from inputs and only what is necessary is sent to the client via Ajax calls. Performance is greatly increased using these methods.  Dynamic TSQL queries are computed to interact with the database. JSON is the format used to communicate datasets via ajax calls and for some db storage.

The [Reporting] pieces are functional for basic aggregated data, but is designed to do a lot more and has unfinished feature areas.  The engine/db (see class [ReportDefinition]) was designed to handle "Subjects" with "Details" to filter down what data you desire on the "Subject."


-- REQUIREMENTS ---------------------------------------------------------------------------------------
  * Microsoft SQL Server back-end (connection strings in web.config)
    - Build a DB and use the "data/DB" project folder queries to structure it
  * IIS (tested on 7)



-- Features ---------------------------------------------------------------------------------------

  * [AUDITING] Form field change tracking "statement" [string] can be saved to your db with every form "Edit" that contains exactly what was changed (from/to) and the user's username.
  * [VERSIONING] Form's save their entire change history (fields added/removed, etc.) with a version # that can be used for various tracking or historical purposes, and especially reporting for pulling in the fields by version.


-- Using -------------------------------------------------------------------------------------

There are two main tools in this framework that build/manage forms and reports.

  1. Designer (GUI for creating/managing forms and their fields)
  2. ReportDesigner (unfinished| GUI for creating reports)

Most reports are built using the engine coupled with specific application code.  The "ReportDesigner" tool was an attempt to provide a user-friendly interface so that users could generate their own reports on the fly and/or to be saved and applied in an application for use. 

The rest of the functionality lies in the respective classes.  The two tools will help you understand how to use them (i.e. the ReportDesigner uses the Form struct to generate its own forms)

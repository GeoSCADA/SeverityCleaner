Please refer to the file LICENSE.txt for terms relating to this code

Severity Cleaner
================
This program and document are Copyright Schneider Electric 2022. All rights reserved. See the license 
terms file in this folder.

Introduction
------------
Alarm priorities are configured as numbers from 1 to 1000. These are mapped in server configuration
to named severities with colors and behaviors. You can remove severities but the numbers used by
them in the database will remain, and when alarms occur the next lowest severity will be used.

However, the severity drop-down field on the database item may appear blank when it is not mapped to a
severity. This tool can be used to:
a) Tidy severities by remapping the priorities which are unmapped to the next lowest severity.
b) Allow you to remap all items using one severity into another severity.

Build
-----
Typically a Geo SCADA program written to use the .Net Client API will require a reference to the dll
"ClearSCADA.Client.dll" in the Program Files\Schneider Electric\ClearSCADA folder. To rebuild for your
version of Geo SCADA you may need to remove and reinstate this reference. To deploy, place your built
executable into the same folder on the same Geo SCADA version in order to run this utility.

Command Line
-------------
Use the command parameter -? to get this help for command line parameters:

Support
-------
This code and build are provided without support. Please refer to the Schneider Electric Exchange web forums 
to get help.

April 2022

You can discuss these features in the SE Exchange forums.

Setup notes
===========

Backtrace extension
-------------------
This enumerates all the file paths of referenced assemblies, given a single target assembly.
The extension project *must* be referenced by the setup project, and the reference must have copy local set.

Use like:

    <Component Id="MainExecutable" Guid="322E3544-6E73-4C27-B5E2-317A34F218A8">
        <File Id="_Product_File" Name="MyProgram.exe" Source="$(var.MyProject.TargetDir)MyProgram.exe" KeyPath="yes"/>
        <?foreach dependency in $(backtrace.dependenciesOf($(var.MyProject.TargetDir)MyProgram.exe))?>
            <File Id="_$(backtrace.id())" Source="$(var.dependency)"/>
        <?endforeach?>
    </Component>

Note: the `Guid` field *must* be supplied when adding multiple files to a component.
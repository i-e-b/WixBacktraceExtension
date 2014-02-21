WiX Experiment
==============

Examples of application and website deployment being built up as I learn WiX.

Includes a 'backtrace' extension that finds all dependencies of a .Net assembly (.dll or .exe)
and exposes this as an enumerable inside the .wxs product XML.


OUT OF DATE
===========
the info below is old, while I experiment with clean was of de-duplicating output.

Backtrace extension
-------------------
This enumerates all the file paths of referenced assemblies, given a single target assembly.
The extension project *must* be referenced by the setup project, and the reference must be
by file path, not in "Projects" (if you use the VS GUI). If you edit the .wixproj file you should see

    <WixExtension Include="WixBacktraceExtension">
      <HintPath>..\WixBacktraceExtension\bin\Debug\WixBacktraceExtension.dll</HintPath>
      <Name>WixBacktraceExtension</Name>
    </WixExtension>

(with the `HintPath` corrected to the relative location of the extension from the wixproj)

Once this is done, you can use a `<?foreach?>` declaration to build components for all the
dependencies of a primary component. (See the example setup projects for full usage)

    <?foreach dependency in $(get.dependenciesOf($(var.MyProject.TargetPath)))?>
        <Component Id="...">
            <File Id="..." Source="$(var.dependency)"/>
        </Component>
    <?endforeach?>

WiX Experiment
==============

Examples of application and website deployment being built up as I learn WiX.

Includes a 'backtrace' extension that finds all dependencies of a .Net assembly (.dll or .exe)
and exposes this as an enumerable inside the .wxs product XML.


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

Once this is done, you can use a `<?pragma?>` declaration to build and reference components
for all the dependencies of a primary component. (See the example setup projects for full usage)

* Building:

        <Fragment>
            <?pragma build.componentsFor "$(var.Plugin)" in "PluginsFolder"?>
            <Component Id="OptionalPlugin" Directory="PluginsFolder">
                <File Id="OptionalPluginDll" Name="OptionalPlugin.dll" Source="$(var.Plugin)" KeyPath="yes"/>
            </Component>
        </Fragment>


* Referencing:

        <Feature Id="PluginFeature" ...>
            <ComponentRef Id="OptionalPlugin"/>
            <?pragma include.componentRefsFor $(var.Plugin)?>
        </Feature>


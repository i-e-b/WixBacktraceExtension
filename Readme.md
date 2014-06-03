WiX Backtrace Extension
=======================

An extension that helps with building installers for .Net applications and websites with WiX.
It's like `Heat`, but simpler and more focused for .Net development.

Contains examples of application and website deployment being built with the backtrace extension.
The backtrace extension exposes several pragma directives that make building installers a lot simpler.

Installing
----------

Add the NuGet package from https://www.nuget.org/packages/WixBacktraceExtension/

`Install-Package WixBacktraceExtension`

The extension dll in the solution's packages folder *must* be referenced by the setup project, and the
reference must be by file path, not in "Projects" (if you use the VS GUI). If you edit the .wixproj
file you should see

    <WixExtension Include="WixBacktraceExtension">
      <HintPath>..\..\packages\WixBacktraceExtension.1.0.X\lib\net40\WixBacktraceExtension.dll</HintPath>
      <Name>WixBacktraceExtension</Name>
    </WixExtension>

(with the `HintPath` corrected to the relative location of the extension from the wixproj)

Once this is done, you can use a `<?pragma?>` declaration to build and reference components
for all the dependencies of a primary component. (See the example setup projects for full usage)

**Important** If you build from sources, use the ILMerged output that is left directly in the bin folder,
not the unmerged one in Debug or Release.

Backtracing for compiled executables
------------------------------------
See `AppSetupProject` for details.

1. Add components and features for primary output as normal (the library or executable for each .csproj).

2. Include dependencies for each primary with the `components.{all|unique}DependenciesOf` pragma:

        <ComponentGroup Id="MainExeDependencies" >
            <?pragma components.uniqueDependenciesOf "$(var.MyProject.TargetPath)" in "INSTALLFOLDER"?>
        </ComponentGroup>

3. Reference the dependencies component group in the primary's feature.


   *Optional*:

4. You can add a condition to a set of traced files using the `if` parameter:

        <?pragma components.uniqueDependenciesOf "..." in "..." if "MYPROP" ?>
   
   This will only install components if the `MYPROP` property is defined. The condition
   can be any WiX condition, such as `&FeatureId = 3` or `PROP <> \"value\"`. The condition
   will be CDATA wrapped, but double-quote characters must be escaped.
   By default, all components are unconditional (they always install).

   *Optional* -- The extension can transform the app.config for your primary output:

5. Add the `components.transformedConfigOf` pragma somewhere in a fragment. You should create the
   Directory node as normal. The plugin will look for a transform file named `Web.{for}.config`
   (in the example below, it will be "Web.Release.config"). This must be copied to the build directory,
   and will be deleted after the transform is complete. You should set your transform files
   to "CopyAlways" in Visual Studio.

        <?pragma components.transformedConfigOf "$(var.MyProject.TargetPath)"
                                            for "Release"
                                         withId "MainExeConfig"
                                             in "INSTALLFOLDER"?>

6. Reference the `{withId}` component group in the primary's feature:

            <Feature Id="MainProgram" ...>
                <ComponentRef Id="MainExecutable"/>
                <ComponentRef Id="MainExeConfig"/>
                <ComponentGroupRef Id="MainExeDependencies" />
            </Feature>

### Notes
* `components.transformedConfigOf` will always overwrite the original \*.config file, so **always** 
  point this at the build output, not the sources.
* `components.uniqueDependenciesOf` will create a component for each version of a dependency
  only **once**. This is usually the correct behaviour for 
  primary executables and their plugins. If there are duplicate dependencies, the first call to 
  `components.uniqueDependenciesOf` gets them. You should always declare the dependencies for the 
  main executable first, before any plugins. `components.allDependenciesOf` will only *store* one
  copy of each version of a dependency, but will copy it as many times as required to deliver each
  target its own set of dependencies.
* If you have multiple trees of dependencies, you can add the `dependencySet = "name"` parameter to `components.uniqueDependenciesOf`.
  Each dependency set is calculated separately. All components without an explicit dependency set name
  will go in a shared default set.
* If you need to use double quotes (`"`) in a backtrace pragma string, use a backslash to escape (`\"`)

Backtracing for WebSite projects
--------------------------------
See `WebsiteSetupProject` for details. The backtrace plugin will compile, publish and compose 
the files for your website, keeping all the folder structure complete. No need to go spelunking
in WiX when you re-arrange your content files!

1. Add components and features for an IIS website (see the demo project's `Product.wxs` for an example

2. Set up a temporary directory for publication output (this should be very early in the .wxs file)

        <?define PublishTemp=$(publish.tempDirectory)?>
   
   This will give a variable `$(var.PublishTemp)` we will use later. The temp folder is created in the 
   system temporary directory, which should get cleaned up by the *Disk Cleanup* utility. If you don't
   want an automatic directory, you can set your own temporary directory in a define and use it instead.

3. Use the `build.directoriesMatching` pragma to build the directory structure for your site

        <Directory Id="SITE_INSTALLFOLDER" Name="MyWebsite">
            <?pragma build.directoriesMatching "$(var.PublishTemp)" withPrefix "SITE" ?>
        </Directory>

   The `withPrefix` is optional, but is useful to save potential naming conflicts.

4. Create a component group for the published site, and include all the website's files

        <ComponentGroup Id="WebsiteContent">
            <?pragma components.publishedWebsiteIn "$(var.PublishTemp)"
                                               for "Release"
                           inDirectoriesWithPrefix "SITE"
                                     rootDirectory "SITE_INSTALLFOLDER"
                                  ignoreExtensions "packages.config|.pfx|.js.map" ?>
        </ComponentGroup>

   The `for` setting is optional (defaults to `"Release"`) and is used to select web.config transforms.
   Each content file from the published site will be matched to the directories created earlier, so the
   prefixes **must** match. Root elements (such as `Global.asax` and `web.config` are created in the 
   directory with id matching the `rootDirectory` parameter (this must be specified).

   The `ignoreExtensions` list is optional, but if provided any files matching the list will be ignored,
   will not be in the output and will not be installed.

   The web.config file will have transforms applied if possible. This is done **before** compiling the
   installer project, so any install-time changes must be done separately.

5. Reference the site content component group in your site feature

        <Feature Id="WebSiteFeature" ...>
            <ComponentGroupRef Id="IisWebSiteSetup" />
            <ComponentGroupRef Id="WebsiteContent" />
        </Feature>

### Notes:
* The auto-generated directories are given 'guessable' ids: `{prefix}` + `_` + *file path* with `_` for
  path separator and any non-id-safe characters
* You can inject files and components into auto-generated directories
  (see the `OptionalPlugin` feature for an example)
* The `components.publishedWebsiteIn` pragma *compiles and publishes* the target project, so you must have
  MSBuild and .Net compiler components installed (this should be the case if you have Visual Studio installed).
* Files in the site root are generated with guessable names: `web.config` will have file `{prefix}_web_config`
  and component `{prefix}_component_web_config`. This is to enable post-install changes (see the example project).
* If you need to use double quotes (`"`) in a backtrace pragma string, use a backslash to escape (`\"`)
* If you have multiple trees of dependencies, you can add the `dependencySet = "name"` parameter to `components.uniqueDependenciesOf`.
  Each dependency set is calculated separately. All components without an explicit dependency set name
  will go in a shared default set.

Bringing in floating DLLs and their dependencies
------------------------------------------------

1. Include target and dependencies with the `components.targetAnd{All|Unique}DependenciesOf` pragma:

        <ComponentGroup Id="MainExeDependencies" >
            <?pragma components.targetAndUniqueDependenciesOf "C:\work\Super.dll" in "INSTALLFOLDER"?>
        </ComponentGroup>

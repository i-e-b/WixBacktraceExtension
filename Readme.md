WiX Experiment
==============

Examples of application and website deployment being built up as I learn WiX.

Includes a 'backtrace' extension that helps with building installers for .Net applications and websites.


Backtrace extension
-------------------
The backtrace extension exposed several pragma directives that make building installers a lot simpler.

The extension project *must* be referenced by the setup project, and the reference must be
by file path, not in "Projects" (if you use the VS GUI). If you edit the .wixproj file you should see

    <WixExtension Include="WixBacktraceExtension">
      <HintPath>..\WixBacktraceExtension\bin\WixBacktraceExtension.dll</HintPath>
      <Name>WixBacktraceExtension</Name>
    </WixExtension>

(with the `HintPath` corrected to the relative location of the extension from the wixproj)

Once this is done, you can use a `<?pragma?>` declaration to build and reference components
for all the dependencies of a primary component. (See the example setup projects for full usage)

**Important** Use the ILMerged output that is left directly in the bin folder,
not the unmerged one in Debug or Release.

Backtracing for compiled executables
------------------------------------
See `AppSetupProject` for details.

1. Add components and features for primary output as normal (the library or executable for each .csproj).

2. Include dependencies for each primary with the `components.uniqueDependenciesOf` pragma:

        <ComponentGroup Id="MainExeDependencies" >
            <?pragma components.uniqueDependenciesOf "$(var.MyProject.TargetPath)" in "INSTALLFOLDER"?>
        </ComponentGroup>

3. Reference the dependencies component group in the primary's feature.


   *Optional* -- The extension can transform the app.config for your primary output:

4. Add the `components.transformedConfigOf` pragma somewhere in a fragment. You should create the
   Directory node as normal. The plugin will look for a transform file named `Web.{for}.config`
   (in the example below, it will be "Web.Release.config"). This must be copied to the build directory,
   and will be deleted after the transform is complete. You should set your transform files
   to "CopyAlways" in Visual Studio.

        <?pragma components.transformedConfigOf "$(var.MyProject.TargetPath)"
                                            for "Release"
                                         withId "MainExeConfig"
                                             in "INSTALLFOLDER"?>

5. Reference the `{withId}` component group in the primary's feature:

            <Feature Id="MainProgram" ...>
                <ComponentRef Id="MainExecutable"/>
                <ComponentRef Id="MainExeConfig"/>
                <ComponentGroupRef Id="MainExeDependencies" />
            </Feature>

### Notes
* `components.transformedConfigOf` will always overwrite the original \*.config file, so **always** 
  point this at the build output, not the sources.
* `components.uniqueDependenciesOf` will create a component fo each version of a dependency
  only **once**. This is usually the correct behaviour for 
  primary executables and their plugins. If there are duplicate dependencies, the first call to 
  `components.uniqueDependenciesOf` gets them. You should always declare the dependencies for the 
  main executable first, before any plugins. If you **need** to duplicate components, use
  `<?pragma components.resetUniqueFilter?>` immediately before `components.uniqueDependenciesOf`.

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
                                     rootDirectory "SITE_INSTALLFOLDER" ?>
        </ComponentGroup>

   The `for` setting is optional (defaults to `"Release"`) and is used to select web.config transforms.
   Each content file from the published site will be matched to the directories created earlier, so the
   prefixes **must** match. Root elements (such as `Global.asax` and `web.config` are created in the 
   directory with id matching the `rootDirectory` parameter (this must be specified).

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



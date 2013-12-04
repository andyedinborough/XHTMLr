$name = "XHTMLr";
function create-nuspec() {    
		$spec = get-text "XHTMLr.nuspec"
		$spec = $spec.Replace("#version#", (get-version("bin\release\$name.dll")))
		$spec = $spec.Replace("#message#", (get-text(".git\COMMIT_EDITMSG")))
		
		$spec | out-file "bin\Package\$name.nuspec"
}

function get-text($file) {
		return [string]::join([environment]::newline, (get-content -path $file))
}

function get-version($file) {
		$ANOTHERONE = resolve-path .
		$file = (join-path "$ANOTHERONE" "$file")
		return [System.Diagnostics.FileVersionInfo]::GetVersionInfo($file).FileVersion
}

del "bin\Package" -recurse
md "bin\Package\lib\net40" 
copy "bin\Release\*.*" "bin\Package\lib\net45"
create-nuspec
.nuget\NuGet.exe pack "bin\Package\$name.nuspec" /o "bin\Package"
allprojects {
    repositories {
        google()
        mavenCentral()
    }
}

val newBuildDir: Directory =
    rootProject.layout.buildDirectory
        .dir("../../build")
        .get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)
}
subprojects {
    project.evaluationDependsOn(":app")
}

// Force a consistent local NDK for all Android library modules (e.g. :jni).
subprojects {
    pluginManager.withPlugin("com.android.library") {
        extensions.findByName("android")?.let { ext ->
            try {
                val setNdk = ext.javaClass.methods.firstOrNull { it.name == "setNdkVersion" && it.parameterCount == 1 }
                setNdk?.invoke(ext, "28.2.13676358")
            } catch (_: Exception) {
                // Best-effort; junction path also covers Flutter's default NDK folder.
            }
        }
    }
}

// Skip broken lintVital tasks from plugins (url_launcher_android CommentDetector crash).
subprojects {
    tasks.configureEach {
        if (name.startsWith("lintVital")) {
            enabled = false
        }
    }
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}

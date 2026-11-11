using Microsoft.VisualStudio.TestTools.UnitTesting;

// Each test method runs on its own instance of its test class, so tests can run side by side.
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

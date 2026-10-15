using Scanner.Page;

namespace Scanner;

public static class Scanner
{
    /// Root object to use for 'solving' a page.
    /// 
    /// Previously, a hideous chain was used instead that was hard to follow.
    /// This new implementation follows what Rust's move semantics would allow,
    /// by repeatedly changing the user facing type of the object at each step,
    /// taking advantage of the compiler to ensure invalid solves are internal bugs.
    ///
    /// This also allows making error handling completely opaque until the very end.
    /// (Except for missing data, which requires user intervention)
    public static IStepPage Begin() => new StepPage();
}
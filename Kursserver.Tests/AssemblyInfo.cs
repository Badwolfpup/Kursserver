// Integration tests share process-wide mutable state (TestAuthHandler.Claims is static and
// is mutated per-test by the authorization tests). Run all tests serially so a test acting as
// a non-Admin role can never bleed into a class that assumes the default Admin identity.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

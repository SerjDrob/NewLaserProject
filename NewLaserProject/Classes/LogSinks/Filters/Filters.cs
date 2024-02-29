namespace NewLaserProject.Classes.LogSinks.Filters
{
    public static class Filters
    {
        public static OnlyForContextFilter<TContext> OnlyForContextFilter<TContext>() => new OnlyForContextFilter<TContext>();
    }
}

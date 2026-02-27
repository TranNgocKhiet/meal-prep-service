namespace MealPrepService.Web.PresentationLayer.Middleware
{
    public static class GlobalExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(options => { });
            return app;
        }
    }
}

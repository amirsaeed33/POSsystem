namespace SmartPos.EntityFrameworkCore.Seed.Host
{
    public class InitialHostDbBuilder
    {
        private readonly SmartPosDbContext _context;

        public InitialHostDbBuilder(SmartPosDbContext context)
        {
            _context = context;
        }

        public void Create()
        {
            new DefaultEditionCreator(_context).Create();
            new DefaultLanguagesCreator(_context).Create();
            new HostRoleAndUserCreator(_context).Create();
            new DefaultSettingsCreator(_context).Create();
            new DefaultSystemAccountsCreator(_context, null).Create();

            _context.SaveChanges();
        }
    }
}

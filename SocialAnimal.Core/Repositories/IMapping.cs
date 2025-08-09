namespace SocialAnimal.Core.Repositories;

public interface IInto<out TRecord> where TRecord : class
{
    TRecord Into();
}

public interface IFrom<in TEntity, out TRecord> 
    where TEntity : class 
    where TRecord : class
{
    static abstract TRecord From(TEntity entity);
}
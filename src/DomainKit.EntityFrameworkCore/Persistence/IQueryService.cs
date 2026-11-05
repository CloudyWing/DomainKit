using System.Linq.Expressions;
using CloudyWing.DomainKit.Paging;
using CloudyWing.DomainKit.Specifications;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

/// <summary>
/// 定義唯讀查詢服務的常用查詢能力。
/// </summary>
/// <typeparam name="TEntity">實體型別。</typeparam>
public interface IQueryService<TEntity> where TEntity : class {
    /// <summary>
    /// 取得或設定一個值，用以指出是否略過軟刪除全域查詢篩選。
    /// </summary>
    /// <remarks>
    /// DomainKit 的 <see cref="QueryService{TDbContext,TEntity}" /> 以 EF Core <c>IgnoreQueryFilters()</c> 實作，
    /// 會一併略過租戶等其他全域查詢篩選，只應用於受控的管理流程，並由呼叫端補上必要的隔離條件。
    /// </remarks>
    bool IgnoreSoftDeletedFilter { get; set; }

    /// <summary>
    /// 判斷是否存在符合條件的資料。
    /// </summary>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>若存在符合條件的資料則為 <see langword="true" />；否則為 <see langword="false" />。</returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 計算符合條件的資料筆數。
    /// </summary>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>資料筆數。</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得符合條件的單筆投影結果。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影結果。</returns>
    Task<TOutput> GetSingleAsync<TOutput>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TOutput>> selector,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 以規約取得符合條件的單筆投影結果。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="specification">查詢規約。</param>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影結果。</returns>
    Task<TOutput> GetSingleAsync<TOutput>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOutput>> selector,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 取得符合條件的單筆投影結果，找不到時回傳 <see langword="null" />。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影結果。</returns>
    Task<TOutput?> GetSingleOrDefaultAsync<TOutput>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TOutput>> selector,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 以規約取得符合條件的單筆投影結果，找不到時回傳 <see langword="null" />。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="specification">查詢規約。</param>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影結果。</returns>
    Task<TOutput?> GetSingleOrDefaultAsync<TOutput>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOutput>> selector,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 取得排序後的第一筆投影結果。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影結果。</returns>
    Task<TOutput> GetFirstAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 以規約取得排序後的第一筆投影結果。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="specification">查詢規約。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影結果。</returns>
    Task<TOutput> GetFirstAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        ISpecification<TEntity> specification,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 取得排序後的第一筆投影結果，找不到時回傳 <see langword="null" />。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影結果。</returns>
    Task<TOutput?> GetFirstOrDefaultAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 以規約取得排序後的第一筆投影結果，找不到時回傳 <see langword="null" />。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="specification">查詢規約。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影結果。</returns>
    Task<TOutput?> GetFirstOrDefaultAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        ISpecification<TEntity> specification,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 取得符合條件的投影清單。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影清單。</returns>
    Task<IReadOnlyList<TOutput>> GetListAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 取得符合條件的分頁投影結果。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="pageNumber">頁碼。</param>
    /// <param name="pageSize">每頁筆數。</param>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>分頁投影結果。</returns>
    /// <remarks>
    /// <paramref name="orderBy" /> 為 <see langword="null" /> 時不套用排序，資料庫不保證各頁的資料順序，可能出現跨頁重複或遺漏。
    /// 分頁查詢應傳入以唯一且穩定的鍵排序的 <paramref name="orderBy" />。
    /// </remarks>
    Task<PagedList<TOutput>> GetPagedListAsync<TOutput>(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, TOutput>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 以規約判斷是否存在符合條件的資料。
    /// </summary>
    /// <param name="specification">查詢規約。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>若存在符合條件的資料則為 <see langword="true" />；否則為 <see langword="false" />。</returns>
    Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// 以規約計算符合條件的資料筆數。
    /// </summary>
    /// <param name="specification">查詢規約。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>資料筆數。</returns>
    Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// 以規約取得投影清單。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="specification">查詢規約。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影清單。</returns>
    Task<IReadOnlyList<TOutput>> GetListAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        ISpecification<TEntity> specification,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 以分頁規約取得分頁投影結果。
    /// </summary>
    /// <typeparam name="TOutput">投影結果型別。</typeparam>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="specification">分頁查詢規約。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>分頁投影結果。</returns>
    Task<PagedList<TOutput>> GetPagedListAsync<TOutput>(
        Expression<Func<TEntity, TOutput>> selector,
        IPagedSpecification<TEntity> specification,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 以分組鍵與彙總投影取得分組結果清單。
    /// </summary>
    /// <typeparam name="TGroupKey">分組鍵型別。</typeparam>
    /// <typeparam name="TOutput">彙總投影型別。</typeparam>
    /// <param name="keySelector">分組鍵選擇器。</param>
    /// <param name="selector">彙總投影選擇器。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="havingPredicate">分組後的篩選條件。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>分組結果清單。</returns>
    /// <remarks>
    /// <typeparamref name="TOutput" /> 的成員必須只包含分組鍵或彙總計算結果，
    /// 否則 EF Core 可能無法將查詢轉譯為 SQL。
    /// </remarks>
    Task<IReadOnlyList<TOutput>> GetGroupedListAsync<TGroupKey, TOutput>(
        Expression<Func<TEntity, TGroupKey>> keySelector,
        Expression<Func<IGrouping<TGroupKey, TEntity>, TOutput>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TOutput, bool>>? havingPredicate = null,
        Func<IQueryable<TOutput>, IOrderedQueryable<TOutput>>? orderBy = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 以規約、分組鍵與彙總投影取得分組結果清單。
    /// </summary>
    /// <typeparam name="TGroupKey">分組鍵型別。</typeparam>
    /// <typeparam name="TOutput">彙總投影型別。</typeparam>
    /// <param name="keySelector">分組鍵選擇器。</param>
    /// <param name="selector">彙總投影選擇器。</param>
    /// <param name="specification">查詢規約。</param>
    /// <param name="havingPredicate">分組後的篩選條件。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>分組結果清單。</returns>
    /// <remarks>
    /// <typeparamref name="TOutput" /> 的成員必須只包含分組鍵或彙總計算結果，
    /// 否則 EF Core 可能無法將查詢轉譯為 SQL。
    /// </remarks>
    Task<IReadOnlyList<TOutput>> GetGroupedListAsync<TGroupKey, TOutput>(
        Expression<Func<TEntity, TGroupKey>> keySelector,
        Expression<Func<IGrouping<TGroupKey, TEntity>, TOutput>> selector,
        ISpecification<TEntity> specification,
        Expression<Func<TOutput, bool>>? havingPredicate = null,
        Func<IQueryable<TOutput>, IOrderedQueryable<TOutput>>? orderBy = null,
        CancellationToken cancellationToken = default
    );
}

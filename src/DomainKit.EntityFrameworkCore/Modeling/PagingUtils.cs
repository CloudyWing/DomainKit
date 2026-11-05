namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

internal static class PagingUtils {
    public static int GetSkipCount(int pageNumber, int pageSize) {
        // 以 long 計算後再收斂至 int 上限，避免大頁碼溢位成負數而回到第一頁。
        long skip = (long)(pageNumber - 1) * pageSize;

        return skip > int.MaxValue ? int.MaxValue : (int)skip;
    }
}

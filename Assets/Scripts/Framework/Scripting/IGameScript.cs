namespace Framework.Scripting
{
    /// <summary>
    /// 所有动态 C# 游戏脚本的入口接口。
    ///
    /// 使用 <see cref="RoslynCompiler"/> 将包含此接口实现的 C# 源码编译为程序集，
    /// 再通过 <see cref="ScriptRunner"/> 自动发现并执行所有实现。
    ///
    /// 示例脚本（在运行时编译）：
    /// <code>
    /// using Framework.Scripting;
    ///
    /// public class HealPlayer : IGameScript
    /// {
    ///     public void Execute(GameScriptContext ctx)
    ///     {
    ///         ctx.Publish(new Framework.Events.PlayerHealthChangedEvent(100, 100));
    ///         ctx.Log("玩家已满血复活");
    ///     }
    /// }
    /// </code>
    /// </summary>
    public interface IGameScript
    {
        /// <summary>
        /// 脚本主逻辑。由 <see cref="ScriptRunner.Run"/> 在主线程调用。
        /// 禁止在此方法内阻塞线程；耗时操作请通过 <see cref="GameScriptContext.EnqueueWork"/> 提交逻辑线程。
        /// </summary>
        void Execute(GameScriptContext context);
    }
}

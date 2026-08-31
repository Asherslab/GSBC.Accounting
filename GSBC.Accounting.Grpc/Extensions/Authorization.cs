using GSBC.Accounting.Grpc.Features.Sessions;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace GSBC.Accounting.Grpc.Extensions;

/// <summary>
/// Registers the anonymous-session scheme and the policies built on it.
/// </summary>
/// <remarks>
/// <b>Deny by default.</b> <c>FallbackPolicy</c> is set to <see cref="Policies.AnonymousSession"/>, so
/// an endpoint that says nothing about authorisation requires a session. That is the whole reason this
/// exists: the per-method ownership checks were already correct everywhere, and a fallback policy is
/// what makes the *next* method correct without anybody remembering.
/// <para>
/// <b>The cost of deny-by-default is that every genuinely open endpoint has to say so out loud</b>, with
/// <c>.AllowAnonymous()</c>. Health checks, the root signpost, <c>Create</c> and the two read-by-id
/// endpoints all carry it. Forgetting one is a 401 in the face rather than a silent hole, which is the
/// right way round for this to fail.
/// </para>
/// <para>
/// <b>No <c>DefaultChallengeScheme</c> beyond this one, and no redirect.</b> Every caller is either the
/// WASM app over grpc-web or a link straight to <c>/api/</c>; a 401 is the answer in both cases, and a
/// login redirect would have nowhere to go.
/// </para>
/// </remarks>
public static class Authorization
{
    public static IServiceCollection AddGsbcAuthorization(this IServiceCollection services)
    {
        services
            .AddAuthentication(AnonymousSessionDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, AnonymousSessionHandler>(
                AnonymousSessionDefaults.AuthenticationScheme, displayName: null, configureOptions: null);

        services.AddAuthorization(options =>
        {
            AuthorizationPolicy anonymousSession = new AuthorizationPolicyBuilder()
                // Pinned to the scheme, not left to the default. When a signed-in scheme lands beside
                // this one, an unpinned policy would start accepting either - which is a decision each
                // endpoint should make explicitly rather than one that arrives with a new registration.
                .AddAuthenticationSchemes(AnonymousSessionDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                // Belt and braces with RequireAuthenticatedUser: the claim is what actually carries the
                // session, and requiring it means a future scheme cannot satisfy this policy by being
                // authenticated while naming no session.
                .RequireClaim(AnonymousSessionDefaults.SessionIdClaim)
                .Build();

            options.AddPolicy(Policies.AnonymousSession, anonymousSession);

            // Deny by default. See the class remarks - this is the point of the exercise.
            options.FallbackPolicy = anonymousSession;
        });

        return services;
    }

    /// <summary>
    /// Exempts named methods on a code-first gRPC service from the authorisation policy.
    /// </summary>
    /// <remarks>
    /// <b><c>[AllowAnonymous]</c> ON A METHOD DOES NOT WORK HERE, and this exists because of it.</b>
    /// protobuf-net.Grpc 1.2.2 does not carry method-level attributes onto the endpoint it builds - it
    /// propagates the service type's attributes and drops the rest - so the attribute compiles, reads
    /// correctly, and is silently ignored.
    /// <para>
    /// Measured, not assumed: with <c>[AllowAnonymous]</c> on <c>Create</c> and nothing else changed,
    /// every method including <c>Create</c> answered <c>HTTP 401</c>. That is a service nobody can ever
    /// obtain a session from, so no draft can ever be created - the whole feature, dead, from an
    /// attribute that looks right.
    /// </para>
    /// <para>
    /// A convention runs against the endpoint after it is built, so the metadata it adds is the metadata
    /// the authorisation middleware actually reads. <c>IAllowAnonymous</c> in endpoint metadata is
    /// checked by that middleware after the policy is resolved and short-circuits it, so this wins over
    /// both the service's <c>[Authorize]</c> and the <c>FallbackPolicy</c>.
    /// </para>
    /// <para>
    /// <b>Throws at startup if a named method does not exist.</b> A typo or a rename would otherwise
    /// leave the method locked with no clue why, and this whole helper is here because a silent failure
    /// in exactly this spot got as far as a running app.
    /// </para>
    /// </remarks>
    public static GrpcServiceEndpointConventionBuilder AllowAnonymousGrpcMethods<TService>(
        this GrpcServiceEndpointConventionBuilder builder,
        params string[] methodNames)
    {
        // Checked against the service type here, not against the endpoints in the convention below.
        // Conventions run once per endpoint as endpoints are built, so a "did anything match?" tally
        // inside one cannot tell "this method does not exist" from "its endpoint has not been built
        // yet" - it would either throw spuriously or, worse, never throw. Reflection answers now.
        foreach (string name in methodNames)
        {
            if (typeof(TService).GetMethod(name) is null)
            {
                throw new InvalidOperationException(
                    $"AllowAnonymousGrpcMethods named '{name}', which does not exist on "
                    + $"{typeof(TService).Name}. It was renamed or misspelled - and the effect of "
                    + "letting that pass is a method silently left behind the authorisation policy.");
            }
        }

        HashSet<string> wanted = new(methodNames, StringComparer.Ordinal);

        builder.Add(endpoint =>
        {
            if (endpoint.Metadata.OfType<GrpcMethodMetadata>().FirstOrDefault() is { } grpc
                && wanted.Contains(grpc.Method.Name))
            {
                endpoint.Metadata.Add(new AllowAnonymousAttribute());
            }
        });

        return builder;
    }
}

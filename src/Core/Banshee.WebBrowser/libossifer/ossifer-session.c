#include <config.h>
#include <webkit/webkit.h>

#ifdef HAVE_LIBSOUP_GNOME
#  include <libsoup/soup-gnome.h>
#endif

typedef struct OssiferSession OssiferSession;

typedef void (* OssiferSessionCookieJarChanged)
    (OssiferSession *session, SoupCookie *old_cookie, SoupCookie *new_cookie);

struct OssiferSession {
    OssiferSessionCookieJarChanged cookie_jar_changed;
};

static void
ossifer_session_cookie_jar_changed (SoupCookieJar *jar,
    SoupCookie *old_cookie, SoupCookie *new_cookie, gpointer user_data)
{
    OssiferSession *session = (OssiferSession *)user_data;
    if (session->cookie_jar_changed != NULL) {
        session->cookie_jar_changed (session, old_cookie, new_cookie);
    }
}

OssiferSession *
ossifer_session_initialize (const gchar *cookie_db_path,
    OssiferSessionCookieJarChanged cookie_jar_changed_callback)
{
    static OssiferSession *session_instance = NULL;

    SoupSession *session;
    SoupCookieJar *cookie_jar;
    gchar *path;

    if (session_instance != NULL) {
        return session_instance;
    }

    session_instance = g_new0 (OssiferSession, 1);
    session_instance->cookie_jar_changed = cookie_jar_changed_callback;

    session = webkit_get_default_session ();

#ifdef HAVE_LIBSOUP_GNOME
    path = g_strdup_printf ("%s.sqlite", cookie_db_path);
    cookie_jar = soup_cookie_jar_sqlite_new (path, FALSE);
#else
    path = g_strdup_printf ("%s.txt", cookie_db_path);
    cookie_jar = soup_cookie_jar_text_new (path, FALSE);
#endif
    soup_session_add_feature (session, SOUP_SESSION_FEATURE (cookie_jar));
    g_object_unref (cookie_jar);
    g_free (path);

    g_signal_connect (cookie_jar, "changed",
        G_CALLBACK (ossifer_session_cookie_jar_changed),
        session_instance);

#ifdef HAVE_LIBSOUP_GNOME
    soup_session_add_feature_by_type (session, SOUP_TYPE_PROXY_RESOLVER_GNOME);
#endif

    return session_instance;
}

void
ossifer_session_set_cookie (const gchar *name, const gchar *value,
    const gchar *domain, const gchar *path, gint max_age)
{
    SoupSession *session;
    SoupCookieJar *cookies;
    SoupCookie *cookie;

    session = webkit_get_default_session ();
    cookies = (SoupCookieJar *)soup_session_get_feature (session, SOUP_TYPE_COOKIE_JAR);

    g_return_if_fail (cookies != NULL);

    cookie = soup_cookie_new (name, value, domain, path, max_age);
    soup_cookie_jar_add_cookie (cookies, cookie);
}
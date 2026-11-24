# Prueba de reversion de migraciones (base de desarrollo local, 2026-11-23)

$ dotnet ef migrations list
20260908035923_InicialSentryGuard
20260908043801_SeedCatalogosYUsuariosPrueba
20260908051535_StaffInternoSinOrganizacion
20261020082813_SeedActivosDemo
20261113131922_AuditoriaCategoriaTransicionesVistas
20261123014615_AgregaRestriccionesCheck

$ dotnet ef database update AuditoriaCategoriaTransicionesVistas   # retrocede la migracion de restricciones CHECK
      Reverting migration '20261123014615_AgregaRestriccionesCheck'.
Reverting migration '20261123014615_AgregaRestriccionesCheck'.
Done.

$ dotnet ef database update   # vuelve a aplicar hasta la ultima
      Applying migration '20261123014615_AgregaRestriccionesCheck'.
Applying migration '20261123014615_AgregaRestriccionesCheck'.
Done.

$ dotnet ef migrations list
20260908035923_InicialSentryGuard
20260908043801_SeedCatalogosYUsuariosPrueba
20260908051535_StaffInternoSinOrganizacion
20261020082813_SeedActivosDemo
20261113131922_AuditoriaCategoriaTransicionesVistas
20261123014615_AgregaRestriccionesCheck

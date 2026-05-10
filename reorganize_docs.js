const fs = require('fs');
const path = require('path');

const mapping = [
    { newNum: 1, oldNum: 1, src: 'doc/01-configuracion-proyectos-dotnet.md', dest: '01-configuracion-proyectos-dotnet.md' },
    { newNum: 2, oldNum: 2, src: 'doc/02-arquitectura-pipeline-http.md', dest: '02-arquitectura-pipeline-http.md' },
    { newNum: 3, oldNum: 3, src: 'doc/03-inyeccion-dependencias.md', dest: '03-inyeccion-dependencias.md' },
    { newNum: 4, oldNum: 4, src: 'doc/04-controladores-rest.md', dest: '04-controladores-rest.md' },
    { newNum: 5, oldNum: 5, src: 'doc/05-validacion-cascada.md', dest: '05-validacion-cascada.md' },
    { newNum: 6, oldNum: 18, src: 'doc/18-rest-best-practices.md', dest: '06-rest-best-practices.md' },
    { newNum: 7, oldNum: 7, src: 'doc/07-repository-pattern.md', dest: '07-repository-pattern.md' },
    { newNum: 8, oldNum: 9, src: 'doc/09-ef-core-postgresql.md', dest: '08-ef-core-postgresql.md' },
    { newNum: 9, oldNum: 10, src: 'doc/10-mongodb.md', dest: '09-mongodb.md' },
    { newNum: 10, oldNum: 11, src: 'doc/11-redis-caching.md', dest: '10-redis-caching.md' },
    { newNum: 11, oldNum: 6, src: 'doc/06-patron-result.md', dest: '11-patron-result.md' },
    { newNum: 13, oldNum: 8, src: 'doc/08-servicios-negocio.md', dest: '13-servicios-negocio.md' },
    { newNum: 15, oldNum: 15, src: 'doc/15-pedidos-transacciones.md', dest: '15-pedidos-transacciones.md' },
    { newNum: 16, oldNum: 22, src: 'doc/22-mapeadores.md', dest: '16-mapeadores.md' },
    { newNum: 17, oldNum: 12, src: 'doc/12-jwt-authentication.md', dest: '17-jwt-authentication.md' },
    { newNum: 18, oldNum: 13, src: 'doc/13-autorizacion-roles.md', dest: '18-autorizacion-roles.md' },
    { newNum: 19, oldNum: 27, src: 'doc/27-seguridad-http.md', dest: '19-seguridad-http.md' },
    { newNum: 20, oldNum: 14, src: 'doc/14-websockets.md', dest: '20-websockets.md' },
    { newNum: 21, oldNum: 20, src: 'doc/20-graphql.md', dest: '21-graphql.md' },
    { newNum: 22, oldNum: 16, src: 'doc/16-file-storage.md', dest: '22-file-storage.md' },
    { newNum: 23, oldNum: 17, src: 'doc/17-email-services.md', dest: '23-email-services.md' },
    { newNum: 24, oldNum: 25, src: 'doc/25-background-jobs.md', dest: '24-background-jobs.md' },
    { newNum: 25, oldNum: 19, src: 'doc/19-documentacion.md', dest: '25-documentacion-api.md' },
    { newNum: 26, oldNum: 21, src: 'doc/21-testing.md', dest: '26-testing.md' },
    { newNum: 27, oldNum: 23, src: 'doc/23-docker.md', dest: '27-docker-ci-cd.md' },
    { newNum: 28, oldNum: 24, src: 'doc/24-logging.md', dest: '28-logging.md' },
    { newNum: 29, oldNum: 26, src: 'doc/26-optimizacion.md', dest: '29-optimizacion.md' },
    { newNum: 30, oldNum: 28, src: 'doc/28-ci-cd.md', dest: '30-ci-cd.md' },
    { newNum: 31, oldNum: 29, src: 'doc/29-clean-architecture.md', dest: '31-clean-architecture.md' },
    { newNum: 32, oldNum: 30, src: 'doc/30-organizacion-program.md', dest: '32-organizacion-program.md' }
];

const targetDir = 'doc/reorganized';

function fixEncoding(content) {
    const map = {
        'Ã³': 'ó', 'Ã¡': 'á', 'Ã©': 'é', 'Ã­': 'í', 'Ãº': 'ú', 'Ã±': 'ñ',
        'Ã“': 'Ó', 'Ã ': 'Á', 'Ã‰': 'É', 'Ã ': 'Í', 'Ãš': 'Ú', 'Ã‘': 'Ñ',
        'Â¿': '¿'
    };
    let fixed = content;
    for (const [bad, good] of Object.entries(map)) {
        fixed = fixed.split(bad).join(good);
    }
    return fixed;
}

function generateGitHubAnchor(text) {
    return text.toLowerCase()
        .replace(/\./g, '')
        .replace(/\s+/g, '-')
        .replace(/[^\w-]/g, (match) => {
             // Handle accented characters if needed, but GitHub handles them by keeping them
             return match;
        });
}

mapping.forEach(file => {
    const srcPath = path.resolve(file.src);
    const destPath = path.resolve(targetDir, file.dest);

    if (!fs.existsSync(srcPath)) {
        console.error(`Source file not found: ${file.src}`);
        return;
    }

    let content = fs.readFileSync(srcPath, 'utf8');
    
    // Fix encoding issues first
    content = fixEncoding(content);

    // Update H1 title: # [OldNum]. [Title] -> # [NewNum]. [Title]
    // Use regex to catch the title line
    const h1Regex = new RegExp(`^# ${file.oldNum}\\.\\s+`, 'm');
    content = content.replace(h1Regex, `# ${file.newNum}. `);

    // Update subtitles: ## [OldNum].[X]. -> ## [NewNum].[X].
    // Catch ## [OldNum].X. and ### [OldNum].X.Y.
    const subRegex = new RegExp(`^(#{2,6})\\s+${file.oldNum}\\.`, 'gm');
    content = content.replace(subRegex, `$1 ${file.newNum}.`);

    // Update TOC link text: [OldNum].X. Texto -> [NewNum].X. Texto
    // Match something like [1.1. Texto] or just 1.1. Texto inside TOC
    // Usually it's in the form: [1.1. Creación...](#11-creación...)
    const tocLinkTextRegex = new RegExp(`\\[${file.oldNum}\\.`, 'g');
    content = content.replace(tocLinkTextRegex, `[${file.newNum}.`);

    // Update anchors: (#OldNumX-texto) -> (#NewNumX-texto)
    // Anchors usually look like (#11-creación...) if the old num was 1
    // We need to be careful not to replace partial numbers.
    // GitHub anchors for "1.1. Texto" is usually "11-texto" or "11-texto-1"
    // Based on the files I saw: (#11-creación-de-soluciones-y-proyectos)
    // So if oldNum is 1, it matches "1" at the start of the anchor.
    // If oldNum is 18, it matches "18" at the start.
    
    // Let's use a regex that looks for (#oldNum...)
    const anchorRegex = new RegExp(`\\(#${file.oldNum}`, 'g');
    content = content.replace(anchorRegex, `(#${file.newNum}`);

    fs.writeFileSync(destPath, content, 'utf8');
    console.log(`Processed: ${file.src} -> ${file.dest}`);
});

SELECT 
	challenges.ChallengeName,
	challenges.category,
	challenges.catboxAlbum,
	images.difficulty,
	images.run,
	images.image_path,
	images.image_url,
	images.creator,
	images.source,
	images.catboxUrl

FROM images
INNER JOIN challenges
ON challenges.id = images.challenge_id
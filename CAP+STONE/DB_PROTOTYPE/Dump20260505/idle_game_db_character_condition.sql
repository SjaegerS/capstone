-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: idle_game_db
-- ------------------------------------------------------
-- Server version	8.0.44

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `character_condition`
--

DROP TABLE IF EXISTS `character_condition`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `character_condition` (
  `condition_id` bigint NOT NULL AUTO_INCREMENT,
  `user_id` bigint NOT NULL,
  `character_type_id` int NOT NULL,
  `condition_score` int DEFAULT '3',
  `condition_grade` enum('BEST','GOOD','NORMAL','BAD','WORST') COLLATE utf8mb4_unicode_ci DEFAULT 'NORMAL',
  `last_updated_date` date NOT NULL,
  PRIMARY KEY (`condition_id`),
  UNIQUE KEY `uq_user_character_condition` (`user_id`,`character_type_id`),
  KEY `fk_character_condition_type` (`character_type_id`),
  CONSTRAINT `fk_character_condition_type` FOREIGN KEY (`character_type_id`) REFERENCES `character_type` (`character_type_id`),
  CONSTRAINT `fk_character_condition_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `chk_condition_score` CHECK ((`condition_score` between 1 and 5))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `character_condition`
--

LOCK TABLES `character_condition` WRITE;
/*!40000 ALTER TABLE `character_condition` DISABLE KEYS */;
INSERT INTO `character_condition` VALUES (1,1,1,3,'NORMAL','2026-05-05'),(2,1,2,3,'NORMAL','2026-05-05'),(3,1,3,3,'NORMAL','2026-05-05'),(4,1,4,3,'NORMAL','2026-05-05');
/*!40000 ALTER TABLE `character_condition` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-05-05 18:08:32
